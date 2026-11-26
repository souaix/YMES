using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc212Repository(NpgsqlDataSource dataSource)
{
    private const string GroupColumns = "groupno,groupname,lotisvalid::int,componentcollect::int,planno,formname,weburl,unionerf::int,errorreportname,noticereportname,issuestate::int,description,creator,createdate,cursormode::int,qcclass,getfilepath,executionfile,datastamp,reviser,revisedate";
    private const int MaxGroupDetails = 1000;

    public IReadOnlyList<Qc212GroupDto> ListGroups(string? functionNo, int? issueState, string? groupNo)
    {
        if (!string.Equals(functionNo, "QCITEM_G", StringComparison.Ordinal)) throw Rule(400, "FunctionNo 必須為 QCITEM_G。");
        ValidateIssueState(issueState);
        var search = OptionalText(groupNo, "GroupNo 查詢", 25) ?? "";
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcitemgroupbasis");
        using var command = new NpgsqlCommand($"SELECT {GroupColumns} FROM \"YMES\".tblqcitemgroupbasis WHERE (@state IS NULL OR issuestate=@state) AND (@search='' OR position(lower(@search) in lower(groupno))>0) ORDER BY groupno LIMIT 2000;", connection);
        Number(command, "state", issueState);
        Text(command, "search", search, 25);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc212GroupDto>();
        while (reader.Read()) rows.Add(ReadGroup(reader));
        return rows;
    }

    public IReadOnlyList<Qc212DetailDto> Details(string groupNo)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        EnsureTables(connection, transaction, "tblqcitemgroupbasis", "tblqcitemgroupdetail", "tblqcitemforvariables", "tblqccalculatescript");
        _ = ReadGroup(connection, transaction, no, false);
        const string sql = """
            SELECT d.groupno,d.qcitemno,v.qcitemname,d.showorder::int,
                   COALESCE(NULLIF(btrim(s.script),''),'')<>'',v.componentcollect::int,v.issuestate::int
            FROM "YMES".tblqcitemgroupdetail d
            LEFT JOIN "YMES".tblqcitemforvariables v ON v.qcitemno=d.qcitemno
            LEFT JOIN "YMES".tblqccalculatescript s ON s.groupno=d.groupno AND s.qcitemno=d.qcitemno
            WHERE d.groupno=@no
            ORDER BY d.showorder,d.qcitemno;
            """;
        using var command = new NpgsqlCommand(sql, connection, transaction);
        Text(command, "no", no, 25);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc212DetailDto>();
        while (reader.Read())
            rows.Add(new(reader.GetString(0), reader.GetString(1), NullableText(reader, 2), reader.GetInt32(3), reader.GetBoolean(4), NullableInt(reader, 5), NullableInt(reader, 6)));
        transaction.Commit();
        return rows;
    }

    public Qc212GroupDto Create(Qc212GroupWriteRequest request, string actor)
    {
        var value = Normalize(request);
        actor = Actor(actor, 10);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTable(connection, transaction, "tblqcitemgroupbasis");
        EnsureNoGroupNoCollision(connection, transaction, value.GroupNo);
        ValidatePlan(connection, transaction, value.PlanNo, value.ComponentCollect);
        const string sql = """
            INSERT INTO "YMES".tblqcitemgroupbasis
                (groupno,groupname,lotisvalid,componentcollect,planno,formname,weburl,unionerf,
                 errorreportname,noticereportname,issuestate,description,creator,createdate,cursormode,
                 qcclass,getfilepath,executionfile,datastamp,reviser,revisedate)
            VALUES
                (@no,@name,@lot,@collect,@plan,@form,@url,@union,@errorReport,@noticeReport,0,@description,@actor,
                 now() AT TIME ZONE 'UTC',@cursor,@class,@path,@execution,0,NULL,NULL)
            RETURNING
            """ + GroupColumns + ";";
        using var command = new NpgsqlCommand(sql, connection, transaction);
        BindGroup(command, value);
        Text(command, "actor", actor, 10);
        var result = ReadReturnedGroup(command);
        transaction.Commit();
        return result;
    }

    public Qc212GroupDto Update(string groupNo, Qc212GroupUpdateRequest request, string actor)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        var value = Normalize(request);
        actor = Actor(actor, 10);
        var expectedStamp = ExpectedStamp(request.DataStamp);
        if (request.IssueState != 0) throw Rule(400, "更新時 IssueState 必須為 0，請重新載入群組資料。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTable(connection, transaction, "tblqcitemgroupbasis");
        var current = ReadGroup(connection, transaction, no, true);
        ExpectDraft(current, expectedStamp);
        ValidatePlan(connection, transaction, value.PlanNo, value.ComponentCollect);
        const string sql = """
            UPDATE "YMES".tblqcitemgroupbasis
               SET groupname=@name,lotisvalid=@lot,componentcollect=@collect,planno=@plan,formname=@form,
                   weburl=@url,unionerf=@union,errorreportname=@errorReport,noticereportname=@noticeReport,
                   description=@description,cursormode=@cursor,qcclass=@class,getfilepath=@path,executionfile=@execution,
                   datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC'
             WHERE groupno=@no AND issuestate=0 AND COALESCE(datastamp,0)=@stamp
            RETURNING
            """ + GroupColumns + ";";
        using var command = new NpgsqlCommand(sql, connection, transaction);
        BindGroup(command, value);
        Text(command, "no", no, 25);
        Text(command, "actor", actor, 10);
        Number(command, "stamp", expectedStamp);
        var result = ReadReturnedGroup(command);
        transaction.Commit();
        return result;
    }

    public void Delete(string groupNo, string actor)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        actor = Actor(actor, 10);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTables(connection, transaction, "tblqcitemgroupbasis");
        var current = ReadGroup(connection, transaction, no, true);
        if (current.IssueState == 1) throw Rule(409, "簽核中的品管項目群組不可刪除。");
        if (current.IssueState == 2)
        {
            using var retire = new NpgsqlCommand("UPDATE \"YMES\".tblqcitemgroupbasis SET issuestate=-1,datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC' WHERE groupno=@no AND issuestate=2 AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction);
            Text(retire, "no", no, 25); Text(retire, "actor", actor, 10); Number(retire, "stamp", current.DataStamp);
            if (retire.ExecuteNonQuery() != 1) throw Conflict();
            transaction.Commit();
            return;
        }
        if (current.IssueState is not (0 or -1)) throw Rule(409, "目前狀態不可刪除。");
        EnsureTables(connection, transaction, "tblwiptemp_qcitem", "tblqcitemgroupdetail", "tblqccalculatescript", "tblqcitemparameter", "tblopqcitem", "xx_tblqcitem_memo");
        using (var usage = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblwiptemp_qcitem WHERE lower(btrim(qcitemno))=lower(btrim(@no)));", connection, transaction))
        {
            Text(usage, "no", no, 25);
            if (usage.ExecuteScalar() is true) throw Rule(409, "此群組仍被 WIP 暫存檢驗資料使用，未刪除。");
        }
        DeleteByGroup(connection, transaction, "tblqcitemgroupdetail", no);
        DeleteByGroup(connection, transaction, "tblqccalculatescript", no);
        DeleteByItem(connection, transaction, "tblqcitemparameter", no);
        DeleteByGroupAndType(connection, transaction, "tblopqcitem", no, 2);
        DeleteByGroupAndType(connection, transaction, "xx_tblqcitem_memo", no, 1);
        using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcitemgroupbasis WHERE groupno=@no AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction))
        {
            Text(delete, "no", no, 25); Number(delete, "state", current.IssueState); Number(delete, "stamp", current.DataStamp);
            if (delete.ExecuteNonQuery() != 1) throw Conflict();
        }
        transaction.Commit();
    }

    public Qc212GroupDto Approve(string groupNo, Qc212ApproveRequest request, string actor)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        actor = Actor(actor, 10);
        var expectedStamp = ExpectedStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTables(connection, transaction, "tblqcitemgroupbasis", "tblqcitemgroupdetail");
        var current = ReadGroup(connection, transaction, no, true);
        ExpectDraft(current, expectedStamp);
        using (var details = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblqcitemgroupdetail WHERE groupno=@no);", connection, transaction))
        {
            Text(details, "no", no, 25);
            if (details.ExecuteScalar() is not true) throw Rule(422, "至少加入一筆品管項目明細後才能核准。");
        }
        if (IssueTablesExist(connection, transaction))
            throw Rule(503, "YMES 已存在 Issue/Job 表，但 QCITEM_G 簽核尚未接入共用流程；資料未變更。");
        using var command = new NpgsqlCommand("UPDATE \"YMES\".tblqcitemgroupbasis SET issuestate=2,datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC' WHERE groupno=@no AND issuestate=0 AND COALESCE(datastamp,0)=@stamp RETURNING " + GroupColumns + ";", connection, transaction);
        Text(command, "no", no, 25); Text(command, "actor", actor, 10); Number(command, "stamp", expectedStamp);
        var result = ReadReturnedGroup(command);
        transaction.Commit();
        return result;
    }

    public void CopyDetails(string sourceGroupNo, Qc212CopyDetailsRequest request)
    {
        var sourceNo = Code(sourceGroupNo, "SourceGroupNo", 25, true)!;
        var targetNo = Code(request.TargetGroupNo, "TargetGroupNo", 25, true)!;
        if (Same(sourceNo, targetNo)) throw Rule(400, "來源與目標群組不可相同。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTables(connection, transaction, "tblqcitemgroupbasis", "tblqcitemgroupdetail", "tblqcitemforvariables", "tblqccalculatescript");
        var source = ReadGroup(connection, transaction, sourceNo, false);
        var target = ReadGroup(connection, transaction, targetNo, true);
        RequireState(target, 0, "只有未凍結目標群組可以複製明細。");
        if (source.ComponentCollect != target.ComponentCollect) throw Rule(422, "來源與目標群組的 ComponentCollect 不同，不能複製明細。");
        var sourceDetails = ReadDetailKeys(connection, transaction, sourceNo);
        if (sourceDetails.Count > MaxGroupDetails) throw Rule(413, $"來源群組明細超過 {MaxGroupDetails} 筆，無法複製。");
        sourceDetails = sourceDetails.Select((row, index) => new Qc212DetailWriteRequest { QcItemNo = row.QcItemNo, ShowOrder = index }).ToList();
        ValidateApprovedItems(connection, transaction, sourceDetails, target.ComponentCollect);
        ReplaceDetails(connection, transaction, targetNo, sourceDetails);
        transaction.Commit();
    }

    public IReadOnlyList<Qc212VariableItemOption> VariableQcItems(int? issueState)
    {
        if (issueState is not null and not (-1 or 0 or 1 or 2)) throw Rule(400, "IssueState 僅允許 -1、0、1、2。");
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcitemforvariables");
        using var command = new NpgsqlCommand("SELECT qcitemno,qcitemname,componentcollect::int,issuestate::int FROM \"YMES\".tblqcitemforvariables WHERE (@state IS NULL OR issuestate=@state) ORDER BY qcitemno LIMIT 5000;", connection);
        Number(command, "state", issueState);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc212VariableItemOption>();
        while (reader.Read()) rows.Add(new(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? 0 : reader.GetInt32(2), reader.GetInt32(3)));
        return rows;
    }

    public IReadOnlyList<Qc212OperationOption> Operations()
    {
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblopbasis");
        using var command = new NpgsqlCommand("SELECT opno,opname,oporder FROM \"YMES\".tblopbasis WHERE issuestate=2 ORDER BY oporder,opno LIMIT 5000;", connection);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc212OperationOption>();
        while (reader.Read()) rows.Add(new(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetDecimal(2)));
        return rows;
    }

    public IReadOnlyList<Qc212ParameterOption> MesParameters(int? issueState, int? parameterClass)
    {
        if (issueState != 2 || parameterClass != 1) throw Rule(400, "此查找只提供已核准 IssueState=2、ParameterClass=1 的 MES Parameter。");
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblsysmesparameter");
        using var command = new NpgsqlCommand("SELECT parameterno,parametername FROM \"YMES\".tblsysmesparameter WHERE issuestate=2 AND parameterclass=1 ORDER BY parameterno LIMIT 5000;", connection);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc212ParameterOption>();
        while (reader.Read()) rows.Add(new(reader.GetString(0), reader.GetString(1)));
        return rows;
    }

    public IReadOnlyList<Qc212InterfaceOption> WinSpcInterfaces()
    {
        using var connection = dataSource.OpenConnection();
        EnsureTables(connection, null, "tblqcitemgroupbasis", "tblqcitemforvariables");
        const string sql = """
            SELECT DISTINCT name,file
            FROM (
                SELECT NULLIF(btrim(formname),'') AS name,NULLIF(btrim(executionfile),'') AS file FROM "YMES".tblqcitemgroupbasis
                UNION
                SELECT NULLIF(btrim(formname),'') AS name,NULLIF(btrim(executionfile),'') AS file FROM "YMES".tblqcitemforvariables
            ) options
            WHERE name IS NOT NULL AND file IS NOT NULL
            ORDER BY name,file LIMIT 1000;
            """;
        using var command = new NpgsqlCommand(sql, connection);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc212InterfaceOption>();
        while (reader.Read()) rows.Add(new(reader.GetString(0), reader.GetString(1)));
        return rows;
    }

    public IReadOnlyList<string> QcClasses()
    {
        using var connection = dataSource.OpenConnection();
        EnsureTables(connection, null, "tblqcitemgroupbasis", "tblqcitemforvariables");
        const string sql = """
            SELECT DISTINCT class_name FROM (
                SELECT NULLIF(btrim(qcclass),'') AS class_name FROM "YMES".tblqcitemgroupbasis
                UNION
                SELECT NULLIF(btrim(qcclass),'') AS class_name FROM "YMES".tblqcitemforvariables
            ) classes
            WHERE class_name IS NOT NULL ORDER BY class_name LIMIT 1000;
            """;
        using var command = new NpgsqlCommand(sql, connection);
        using var reader = command.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read()) rows.Add(reader.GetString(0));
        return rows;
    }

    public IReadOnlyList<Qc212ParameterDto> Parameters(string groupNo)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        EnsureTables(connection, transaction, "tblqcitemgroupbasis", "tblqcitemparameter", "tblsysmesparameter");
        _ = ReadGroup(connection, transaction, no, false);
        const string sql = """
            SELECT p.parameterno,p.parametername,0,COALESCE(q.parametervalue,'')
            FROM "YMES".tblsysmesparameter p
            LEFT JOIN "YMES".tblqcitemparameter q ON q.qcitemno=@no AND q.parameterno=p.parameterno
            WHERE p.issuestate=2 AND p.parameterclass=1
            ORDER BY p.parameterno LIMIT 5000;
            """;
        using var command = new NpgsqlCommand(sql, connection, transaction);
        Text(command, "no", no, 25);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc212ParameterDto>();
        while (reader.Read()) rows.Add(new(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3)));
        transaction.Commit();
        return rows;
    }

    public void ReplaceParameters(string groupNo, IReadOnlyList<Qc212ParameterWriteRequest>? requestRows)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        var rows = requestRows ?? throw Rule(400, "Parameter 清單不可為空值。");
        if (rows.Count > 5000) throw Rule(413, "Parameter 清單最多 5000 筆。");
        var normalized = rows.Select(row =>
        {
            if (row.SpecType != 0) throw Rule(422, "MES 的 TBLQCITEMPARAMETER 沒有 SpecType 欄位，目前只接受固定值（0）。");
            return new Qc212ParameterWriteRequest
            {
                ParameterNo = Code(row.ParameterNo, "ParameterNo", 20, true)!,
                SpecType = 0,
                Value = OptionalText(row.Value, "ParameterValue", 255, multiline: true)
            };
        }).ToList();
        EnsureUnique(normalized.Select(row => row.ParameterNo), "ParameterNo");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTables(connection, transaction, "tblqcitemgroupbasis", "tblqcitemparameter", "tblsysmesparameter");
        _ = ReadGroup(connection, transaction, no, true);
        var canonical = ApprovedParameterCodes(connection, transaction, normalized.Select(row => row.ParameterNo));
        if (canonical.Count != normalized.Count) throw Rule(422, "Parameter 必須是已核准且 ParameterClass=1 的 MES 參數。");
        using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcitemparameter WHERE qcitemno=@no;", connection, transaction))
        {
            Text(delete, "no", no, 25); delete.ExecuteNonQuery();
        }
        foreach (var row in normalized)
        {
            var parameterNo = canonical.Single(value => Same(value, row.ParameterNo));
            using var insert = new NpgsqlCommand("INSERT INTO \"YMES\".tblqcitemparameter(qcitemno,parameterno,parametervalue) VALUES(@no,@parameter,@value);", connection, transaction);
            Text(insert, "no", no, 25); Text(insert, "parameter", parameterNo, 20); Text(insert, "value", row.Value, 255); insert.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public IReadOnlyList<string> OperationsForGroup(string groupNo)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        EnsureTables(connection, transaction, "tblqcitemgroupbasis", "tblopqcitem");
        _ = ReadGroup(connection, transaction, no, false);
        using var command = new NpgsqlCommand("SELECT opno FROM \"YMES\".tblopqcitem WHERE qcitemno=@no AND qcitemtype=2 ORDER BY executeorder,opno;", connection, transaction);
        Text(command, "no", no, 25);
        using var reader = command.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read()) rows.Add(reader.GetString(0));
        transaction.Commit();
        return rows;
    }

    public void ReplaceOperations(string groupNo, Qc212OperationsWriteRequest request, string actor)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        if (request.QcType != 2) throw Rule(400, "QCType 必須為 2。");
        actor = Actor(actor, 10);
        var requested = request.OpNos ?? throw Rule(400, "OPNo 清單不可為空值。");
        if (requested.Count > 1000) throw Rule(413, "作業站最多可選 1000 筆。");
        var opNos = requested.Select(value => Code(value, "OPNo", 20, true)!).ToList();
        EnsureUnique(opNos, "OPNo");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTables(connection, transaction, "tblqcitemgroupbasis", "tblopqcitem", "tblopbasis");
        var group = ReadGroup(connection, transaction, no, true);
        if (group.IssueState is 0 or 1) throw Rule(409, "未凍結或簽核中的群組不可修改作業站對應。");
        var canonical = ApprovedOperations(connection, transaction, opNos);
        if (canonical.Count != opNos.Count) throw Rule(422, "作業站必須存在且已核准。");
        using (var types = new NpgsqlCommand("SELECT DISTINCT qcitemtype::int FROM \"YMES\".tblopqcitem WHERE qcitemno=@no;", connection, transaction))
        {
            Text(types, "no", no, 25);
            using var reader = types.ExecuteReader();
            while (reader.Read())
                if (reader.GetInt32(0) != 2) throw Rule(409, "現有作業站對應含其他 QCItemType，不能以群組設定覆寫。");
        }
        using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblopqcitem WHERE qcitemno=@no AND qcitemtype=2;", connection, transaction))
        {
            Text(delete, "no", no, 25); delete.ExecuteNonQuery();
        }
        for (var index = 0; index < opNos.Count; index++)
        {
            using var insert = new NpgsqlCommand("INSERT INTO \"YMES\".tblopqcitem(opno,qcitemno,qcitemtype,inline,executeorder,reviser,revisedate) VALUES(@op,@no,2,1,@order,@actor,now() AT TIME ZONE 'UTC');", connection, transaction);
            Text(insert, "op", canonical.Single(value => Same(value, opNos[index])), 20); Text(insert, "no", no, 25);
            Number(insert, "order", index); Text(insert, "actor", actor, 10); insert.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public Qc212ScriptDto Script(string groupNo, string qcItemNo)
    {
        var group = Code(groupNo, "GroupNo", 25, true)!;
        var item = Code(qcItemNo, "QCItemNo", 25, true)!;
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        EnsureTables(connection, transaction, "tblqcitemgroupbasis", "tblqcitemgroupdetail", "tblqccalculatescript");
        _ = ReadGroup(connection, transaction, group, false);
        EnsureDetail(connection, transaction, group, item);
        using var command = new NpgsqlCommand("SELECT qcitemno,groupno,script,description FROM \"YMES\".tblqccalculatescript WHERE groupno=@group AND qcitemno=@item;", connection, transaction);
        Text(command, "group", group, 25); Text(command, "item", item, 25);
        using var reader = command.ExecuteReader();
        var result = reader.Read()
            ? new Qc212ScriptDto(group, item, NullableText(reader, 2) ?? "", NullableText(reader, 3) ?? "")
            : new Qc212ScriptDto(group, item, "", "");
        transaction.Commit();
        return result;
    }

    public Qc212ScriptDto SaveScript(string groupNo, string qcItemNo, Qc212ScriptWriteRequest request, string actor)
    {
        var groupNoValue = Code(groupNo, "GroupNo", 25, true)!;
        var qcItemNoValue = Code(qcItemNo, "QCItemNo", 25, true)!;
        if (!Same(request.GroupNo, groupNoValue) || !Same(request.QcItemNo, qcItemNoValue)) throw Rule(400, "路徑與腳本內容中的群組／品管項目編號不一致。");
        var script = OptionalText(request.Script, "Script", 255, multiline: true) ?? "";
        var description = OptionalText(request.Description, "Description", 255, multiline: true);
        actor = Actor(actor, 10);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTables(connection, transaction, "tblqcitemgroupbasis", "tblqcitemgroupdetail", "tblqccalculatescript");
        var group = ReadGroup(connection, transaction, groupNoValue, true);
        RequireState(group, 0, "只有未凍結群組可以修改計算腳本。");
        EnsureDetail(connection, transaction, groupNoValue, qcItemNoValue);
        const string sql = """
            INSERT INTO "YMES".tblqccalculatescript(qcitemno,groupno,script,creator,createdate,description)
            VALUES(@item,@group,@script,@actor,now() AT TIME ZONE 'UTC',@description)
            ON CONFLICT(qcitemno,groupno) DO UPDATE SET script=EXCLUDED.script,description=EXCLUDED.description
            RETURNING qcitemno,groupno,script,description;
            """;
        using var command = new NpgsqlCommand(sql, connection, transaction);
        Text(command, "item", qcItemNoValue, 25); Text(command, "group", groupNoValue, 25); Text(command, "script", script, 255);
        Text(command, "actor", actor, 10); Text(command, "description", description, 255);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Conflict();
        var result = new Qc212ScriptDto(reader.GetString(0), reader.GetString(1), NullableText(reader, 2) ?? "", NullableText(reader, 3) ?? "");
        transaction.Commit();
        return result;
    }

    public IReadOnlyList<string> MemoFields(string groupNo)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        EnsureTables(connection, transaction, "tblqcitemgroupbasis", "xx_tblqcitem_memo");
        _ = ReadGroup(connection, transaction, no, false);
        using var command = new NpgsqlCommand("SELECT memo_item FROM \"YMES\".xx_tblqcitem_memo WHERE qcitemno=@no AND qcitemtype=1 ORDER BY memo_item;", connection, transaction);
        Text(command, "no", no, 25);
        using var reader = command.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read()) rows.Add(reader.GetString(0));
        transaction.Commit();
        return rows;
    }

    public void ReplaceMemoFields(string groupNo, Qc212MemoWriteRequest request, string actor)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        if (request.QcItemType != 1) throw Rule(400, "Memo 設定的 QCItemType 固定為 1。");
        actor = Actor(actor, 50);
        var requested = request.ParameterNos ?? throw Rule(400, "Memo 欄位清單不可為空值。");
        if (requested.Count > 1000) throw Rule(413, "Memo 欄位最多可選 1000 筆。");
        var parameterNos = requested.Select(value => Code(value, "ParameterNo", 30, true)!).ToList();
        EnsureUnique(parameterNos, "ParameterNo");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTables(connection, transaction, "tblqcitemgroupbasis", "xx_tblqcitem_memo", "tblsysmesparameter");
        _ = ReadGroup(connection, transaction, no, true);
        var canonical = ApprovedMemoParameterCodes(connection, transaction, parameterNos);
        if (canonical.Count != parameterNos.Count) throw Rule(422, "Memo 欄位必須是已核准且 ParameterClass=1 的 MES 參數。");
        using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".xx_tblqcitem_memo WHERE qcitemno=@no AND qcitemtype=1;", connection, transaction))
        {
            Text(delete, "no", no, 50); delete.ExecuteNonQuery();
        }
        foreach (var parameterNo in canonical)
        {
            using var insert = new NpgsqlCommand("INSERT INTO \"YMES\".xx_tblqcitem_memo(qcitemno,qcitemtype,memo_item,resvitor,\"datetime\") VALUES(@no,1,@item,@actor,now() AT TIME ZONE 'UTC');", connection, transaction);
            Text(insert, "no", no, 50); Text(insert, "item", parameterNo, 50); Text(insert, "actor", actor, 50); insert.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public void ReplaceDetails(string groupNo, Qc212DetailReplaceRequest request)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        var rows = request.Items ?? throw Rule(400, "品管項目明細清單不可為空值。");
        if (rows.Count > MaxGroupDetails) throw Rule(413, $"群組明細最多 {MaxGroupDetails} 筆。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTables(connection, transaction, "tblqcitemgroupbasis", "tblqcitemgroupdetail", "tblqcitemforvariables", "tblqccalculatescript");
        var group = ReadGroup(connection, transaction, no, true);
        RequireState(group, 0, "只有未凍結群組可以修改品管項目明細。");
        var normalized = NormalizeDetailRows(rows);
        ValidateApprovedItems(connection, transaction, normalized, group.ComponentCollect);
        ReplaceDetails(connection, transaction, no, normalized);
        transaction.Commit();
    }

    public void RemoveDetail(string groupNo, string qcItemNo)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        var item = Code(qcItemNo, "QCItemNo", 25, true)!;
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTables(connection, transaction, "tblqcitemgroupbasis", "tblqcitemgroupdetail", "tblqccalculatescript");
        var group = ReadGroup(connection, transaction, no, true);
        RequireState(group, 0, "只有未凍結群組可以移除品管項目明細。");
        EnsureDetail(connection, transaction, no, item);
        using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcitemgroupdetail WHERE groupno=@group AND qcitemno=@item;", connection, transaction))
        {
            Text(delete, "group", no, 25); Text(delete, "item", item, 25); if (delete.ExecuteNonQuery() != 1) throw Rule(404, "找不到該群組的品管項目明細。");
        }
        using (var deleteScript = new NpgsqlCommand("DELETE FROM \"YMES\".tblqccalculatescript WHERE groupno=@group AND qcitemno=@item;", connection, transaction))
        {
            Text(deleteScript, "group", no, 25); Text(deleteScript, "item", item, 25); deleteScript.ExecuteNonQuery();
        }
        using (var reorder = new NpgsqlCommand("WITH ordered AS (SELECT groupno,qcitemno,row_number() OVER(ORDER BY showorder,qcitemno)-1 AS next_order FROM \"YMES\".tblqcitemgroupdetail WHERE groupno=@group) UPDATE \"YMES\".tblqcitemgroupdetail d SET showorder=ordered.next_order FROM ordered WHERE d.groupno=ordered.groupno AND d.qcitemno=ordered.qcitemno;", connection, transaction))
        {
            Text(reorder, "group", no, 25); reorder.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    private static IReadOnlyList<Qc212DetailWriteRequest> ReadDetailKeys(NpgsqlConnection connection, NpgsqlTransaction transaction, string groupNo)
    {
        using var command = new NpgsqlCommand("SELECT qcitemno,showorder::int FROM \"YMES\".tblqcitemgroupdetail WHERE groupno=@no ORDER BY showorder,qcitemno;", connection, transaction);
        Text(command, "no", groupNo, 25);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc212DetailWriteRequest>();
        while (reader.Read()) rows.Add(new() { QcItemNo = reader.GetString(0), ShowOrder = reader.GetInt32(1) });
        return rows;
    }

    private static List<Qc212DetailWriteRequest> NormalizeDetailRows(IEnumerable<Qc212DetailWriteRequest> rows)
    {
        var normalized = rows.Select(row =>
        {
            if (row.ShowOrder is null or < 0 or > 999) throw Rule(400, "ShowOrder 必須介於 0 到 999。");
            return new Qc212DetailWriteRequest { QcItemNo = Code(row.QcItemNo, "QCItemNo", 25, true)!, ShowOrder = row.ShowOrder };
        }).ToList();
        EnsureUnique(normalized.Select(row => row.QcItemNo), "QCItemNo");
        var orders = normalized.Select(row => row.ShowOrder!.Value).Order().ToArray();
        if (orders.Where((value, index) => value != index).Any()) throw Rule(400, "ShowOrder 必須由 0 開始且連續，不可重複。");
        return normalized;
    }

    private static void ValidateApprovedItems(NpgsqlConnection connection, NpgsqlTransaction transaction, IReadOnlyList<Qc212DetailWriteRequest> rows, int? componentCollect)
    {
        if (componentCollect is not (0 or 1)) throw Rule(409, "群組 ComponentCollect 無效，請先修正群組資料。");
        var values = rows.Select(row => row.QcItemNo).ToArray();
        var found = new Dictionary<string, (string Canonical, int? Collect, int? State)>(StringComparer.OrdinalIgnoreCase);
        using var command = new NpgsqlCommand("SELECT qcitemno,componentcollect::int,issuestate::int FROM \"YMES\".tblqcitemforvariables WHERE lower(btrim(qcitemno))=ANY(@items) ORDER BY lower(btrim(qcitemno)) COLLATE \"C\",qcitemno FOR SHARE;", connection, transaction);
        command.Parameters.Add("items", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = values.Select(value => value.Trim().ToLowerInvariant()).ToArray();
        using (var reader = command.ExecuteReader())
            while (reader.Read())
            {
                var canonical = reader.GetString(0);
                if (!found.TryAdd(canonical.Trim(), (canonical, NullableInt(reader, 1), NullableInt(reader, 2))))
                    throw Rule(409, "變數型品管項目存在大小寫／空白歧義。");
            }
        foreach (var row in rows)
        {
            if (!found.TryGetValue(row.QcItemNo.Trim(), out var item)) throw Rule(422, $"找不到計量品管項目 {row.QcItemNo}。");
            if (item.State != 2) throw Rule(422, $"計量品管項目 {row.QcItemNo} 尚未核准。");
            if (item.Collect != componentCollect) throw Rule(422, $"計量品管項目 {row.QcItemNo} 的 ComponentCollect 與群組不符。");
            row.QcItemNo = item.Canonical;
        }
    }

    private static void ReplaceDetails(NpgsqlConnection connection, NpgsqlTransaction transaction, string groupNo, IReadOnlyList<Qc212DetailWriteRequest> rows)
    {
        var retained = rows.Select(row => row.QcItemNo).ToArray();
        using (var cleanup = new NpgsqlCommand("DELETE FROM \"YMES\".tblqccalculatescript WHERE groupno=@group AND NOT (qcitemno=ANY(@items));", connection, transaction))
        {
            Text(cleanup, "group", groupNo, 25); cleanup.Parameters.Add("items", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = retained; cleanup.ExecuteNonQuery();
        }
        using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcitemgroupdetail WHERE groupno=@group;", connection, transaction))
        {
            Text(delete, "group", groupNo, 25); delete.ExecuteNonQuery();
        }
        foreach (var row in rows)
        {
            using var insert = new NpgsqlCommand("INSERT INTO \"YMES\".tblqcitemgroupdetail(groupno,qcitemno,showorder) VALUES(@group,@item,@order);", connection, transaction);
            Text(insert, "group", groupNo, 25); Text(insert, "item", row.QcItemNo, 25); Number(insert, "order", row.ShowOrder); insert.ExecuteNonQuery();
        }
    }

    private static IReadOnlyList<string> ApprovedParameterCodes(NpgsqlConnection connection, NpgsqlTransaction transaction, IEnumerable<string> parameterNos)
    {
        var values = parameterNos.ToArray();
        if (values.Length == 0) return [];
        const string sql = "SELECT parameterno FROM \"YMES\".tblsysmesparameter WHERE lower(btrim(parameterno))=ANY(@items) AND issuestate=2 AND parameterclass=1 ORDER BY lower(btrim(parameterno)) COLLATE \"C\",parameterno FOR SHARE;";
        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add("items", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = values.Select(value => value.Trim().ToLowerInvariant()).ToArray();
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            var name = reader.GetString(0);
            if (!seen.Add(name.Trim())) throw Rule(409, "MES ParameterNo 存在大小寫／空白歧義。");
            result.Add(name);
        }
        return result;
    }

    private static IReadOnlyList<string> ApprovedMemoParameterCodes(NpgsqlConnection connection, NpgsqlTransaction transaction, IEnumerable<string> parameterNos)
    {
        var values = parameterNos.ToArray();
        if (values.Length == 0) return [];
        const string sql = "SELECT parameterno FROM \"YMES\".tblsysmesparameter WHERE lower(btrim(parameterno))=ANY(@items) AND issuestate=2 AND parameterclass=1 ORDER BY lower(btrim(parameterno)) COLLATE \"C\",parameterno FOR SHARE;";
        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add("items", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = values.Select(value => value.Trim().ToLowerInvariant()).ToArray();
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            var name = reader.GetString(0);
            if (!seen.Add(name.Trim())) throw Rule(409, "MES ParameterNo 存在大小寫／空白歧義。");
            result.Add(name);
        }
        return result;
    }

    private static IReadOnlyList<string> ApprovedOperations(NpgsqlConnection connection, NpgsqlTransaction transaction, IEnumerable<string> opNos)
    {
        var values = opNos.ToArray();
        if (values.Length == 0) return [];
        using var command = new NpgsqlCommand("SELECT opno,issuestate::int FROM \"YMES\".tblopbasis WHERE lower(btrim(opno))=ANY(@items) ORDER BY lower(btrim(opno)) COLLATE \"C\",opno FOR SHARE;", connection, transaction);
        command.Parameters.Add("items", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = values.Select(value => value.Trim().ToLowerInvariant()).ToArray();
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            var name = reader.GetString(0);
            if (!seen.Add(name.Trim())) throw Rule(409, "OPNo 存在大小寫／空白歧義。");
            if (reader.GetInt32(1) == 2) result.Add(name);
        }
        return result;
    }

    private static Qc212GroupWriteValue Normalize(Qc212GroupWriteRequest request)
    {
        var no = Code(request.GroupNo, "GroupNo", 25, true)!;
        var name = Code(request.GroupName, "GroupName", 50, true)!;
        var lot = Flag(request.LotIsValid, "LotIsValid");
        var collect = Flag(request.ComponentCollect, "ComponentCollect");
        var union = Flag(request.UnionErf, "UnionERF");
        var cursor = Flag(request.CursorMode, "CursorMode");
        if (collect == 1 && cursor != 0) throw Rule(422, "Wafer 群組的 CursorMode 必須為 Down（0）。");
        return new(
            no, name, lot, collect, Code(request.PlanNo, "PlanNo", 25, false),
            OptionalText(request.FormName, "FormName", 50), OptionalText(request.WebUrl, "WebURL", 100), union,
            OptionalText(request.ErrorReportName, "ErrorReportName", 50), OptionalText(request.NoticeReportName, "NoticeReportName", 50),
            OptionalText(request.Description, "Description", 255, multiline: true), cursor,
            OptionalText(request.QcClass, "QCClass", 30), OptionalText(request.GetFilePath, "GetFilePath", 50),
            OptionalText(request.ExecutionFile, "ExecutionFile", 50));
    }

    private static void BindGroup(NpgsqlCommand command, Qc212GroupWriteValue value)
    {
        Text(command, "no", value.GroupNo, 25); Text(command, "name", value.GroupName, 50); Number(command, "lot", value.LotIsValid);
        Number(command, "collect", value.ComponentCollect); Text(command, "plan", value.PlanNo, 25); Text(command, "form", value.FormName, 50);
        Text(command, "url", value.WebUrl, 100); Number(command, "union", value.UnionErf); Text(command, "errorReport", value.ErrorReportName, 50);
        Text(command, "noticeReport", value.NoticeReportName, 50); Text(command, "description", value.Description, 255);
        Number(command, "cursor", value.CursorMode); Text(command, "class", value.QcClass, 30); Text(command, "path", value.GetFilePath, 50);
        Text(command, "execution", value.ExecutionFile, 50);
    }

    private static void ValidatePlan(NpgsqlConnection connection, NpgsqlTransaction transaction, string? planNo, int componentCollect)
    {
        if (string.IsNullOrWhiteSpace(planNo)) return;
        if (componentCollect == 0)
        {
            _ = QcSamplingPlanReference.ReadGeneral(connection, transaction, planNo, 0, 2, Rule);
            return;
        }
        EnsureTable(connection, transaction, "tblqcsamplingplan_wafer");
        using var command = new NpgsqlCommand("SELECT planno FROM \"YMES\".tblqcsamplingplan_wafer WHERE lower(btrim(planno))=lower(btrim(@no)) AND issuestate=2 ORDER BY planno FOR SHARE;", connection, transaction);
        Text(command, "no", planNo, 25);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(422, "Wafer 抽樣計畫不存在或尚未核准。");
        if (reader.Read()) throw Rule(409, "Wafer PlanNo 存在大小寫／空白歧義。");
    }

    private static Qc212GroupDto ReadGroup(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, bool forUpdate)
    {
        using var command = new NpgsqlCommand($"SELECT {GroupColumns} FROM \"YMES\".tblqcitemgroupbasis WHERE lower(btrim(groupno))=lower(btrim(@no)) ORDER BY groupno" + (forUpdate ? " FOR UPDATE;" : " FOR SHARE;"), connection, transaction);
        Text(command, "no", no, 25);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到品管項目群組，請重新查詢。");
        var result = ReadGroup(reader);
        if (reader.Read()) throw Rule(409, "GroupNo 存在大小寫／空白歧義，請先整理 YMES 主檔。");
        return result;
    }

    private static Qc212GroupDto ReadGroup(NpgsqlDataReader reader) => new(
        reader.GetString(0), NullableText(reader, 1), NullableInt(reader, 2), NullableInt(reader, 3), NullableText(reader, 4),
        NullableText(reader, 5), NullableText(reader, 6), NullableInt(reader, 7), NullableText(reader, 8), NullableText(reader, 9),
        NullableInt(reader, 10), NullableText(reader, 11), NullableText(reader, 12), Date(reader, 13), NullableText(reader, 19),
        Date(reader, 20), NullableInt(reader, 14), NullableText(reader, 15), NullableText(reader, 16), NullableText(reader, 17),
        reader.IsDBNull(18) ? null : reader.GetDecimal(18));

    private static Qc212GroupDto ReadReturnedGroup(NpgsqlCommand command)
    {
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Conflict();
        var result = ReadGroup(reader);
        if (reader.Read()) throw Rule(409, "GroupNo 存在重複資料，請先整理 YMES 主檔。");
        return result;
    }

    private static void EnsureNoGroupNoCollision(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblqcitemgroupbasis WHERE lower(btrim(groupno))=lower(btrim(@no));)", connection, transaction);
        Text(command, "no", no, 25);
        if (command.ExecuteScalar() is true) throw Rule(409, "GroupNo 已存在。");
    }

    private static void ExpectDraft(Qc212GroupDto group, decimal expectedStamp)
    {
        if (group.IssueState != 0) throw Rule(409, "只有未凍結群組可以修改或核准。");
        if ((group.DataStamp ?? 0m) != expectedStamp) throw Conflict();
    }

    private static void RequireState(Qc212GroupDto group, int expected, string message)
    {
        if (group.IssueState != expected) throw Rule(409, message);
    }

    private static void EnsureDetail(NpgsqlConnection connection, NpgsqlTransaction transaction, string groupNo, string qcItemNo)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblqcitemgroupdetail WHERE groupno=@group AND qcitemno=@item);", connection, transaction);
        Text(command, "group", groupNo, 25); Text(command, "item", qcItemNo, 25);
        if (command.ExecuteScalar() is not true) throw Rule(404, "找不到該群組的品管項目明細。");
    }

    private static bool IssueTablesExist(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM unnest(@names::text[]) AS names(name) WHERE to_regclass(format('%I.%I','YMES',name)) IS NOT NULL);", connection, transaction);
        command.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" };
        return command.ExecuteScalar() is true;
    }

    private static void DeleteByGroup(NpgsqlConnection connection, NpgsqlTransaction transaction, string table, string groupNo)
    {
        // Only the fixed table names below are passed; no request input is used as an identifier.
        var sql = table switch
        {
            "tblqcitemgroupdetail" => "DELETE FROM \"YMES\".tblqcitemgroupdetail WHERE groupno=@no;",
            "tblqccalculatescript" => "DELETE FROM \"YMES\".tblqccalculatescript WHERE groupno=@no;",
            _ => throw new InvalidOperationException("Unapproved QC212 group cleanup table.")
        };
        using var command = new NpgsqlCommand(sql, connection, transaction);
        Text(command, "no", groupNo, 25); command.ExecuteNonQuery();
    }

    private static void DeleteByItem(NpgsqlConnection connection, NpgsqlTransaction transaction, string table, string itemNo)
    {
        var sql = table switch
        {
            "tblqcitemparameter" => "DELETE FROM \"YMES\".tblqcitemparameter WHERE qcitemno=@no;",
            _ => throw new InvalidOperationException("Unapproved QC212 item cleanup table.")
        };
        using var command = new NpgsqlCommand(sql, connection, transaction);
        Text(command, "no", itemNo, 50); command.ExecuteNonQuery();
    }

    private static void DeleteByGroupAndType(NpgsqlConnection connection, NpgsqlTransaction transaction, string table, string itemNo, int type)
    {
        var sql = table switch
        {
            "tblopqcitem" => "DELETE FROM \"YMES\".tblopqcitem WHERE qcitemno=@no AND qcitemtype=@type;",
            "xx_tblqcitem_memo" => "DELETE FROM \"YMES\".xx_tblqcitem_memo WHERE qcitemno=@no AND qcitemtype=@type;",
            _ => throw new InvalidOperationException("Unapproved QC212 typed cleanup table.")
        };
        using var command = new NpgsqlCommand(sql, connection, transaction);
        Text(command, "no", itemNo, table == "xx_tblqcitem_memo" ? 50 : 25); Number(command, "type", type); command.ExecuteNonQuery();
    }

    private static void ValidateIssueState(int? issueState)
    {
        if (issueState is not null and not (-1 or 0 or 1 or 2)) throw Rule(400, "IssueState 僅允許 -1、0、1、2。");
    }

    private static decimal ExpectedStamp(decimal? stamp)
    {
        if (!stamp.HasValue || decimal.Truncate(stamp.Value) != stamp.Value || stamp.Value is < 0 or > 99999999999m)
            throw Rule(400, "必須提供 11 位數範圍內的整數 DataStamp。");
        return stamp.Value;
    }

    private static int Flag(int? value, string field)
    {
        if (value is not (0 or 1)) throw Rule(400, $"{field} 必須為 0 或 1。");
        return value.Value;
    }

    private static string Actor(string? value, int maxLength)
    {
        var actor = (value ?? "").Trim();
        if (actor.Length == 0 || actor.Length > maxLength || actor.Any(char.IsControl)) throw Rule(401, "登入者編號無效，無法寫入稽核欄位。");
        return actor;
    }

    private static string? Code(string? value, string field, int maxLength, bool required)
    {
        var text = (value ?? "").Trim();
        if (text.Length == 0) return required ? throw Rule(400, $"{field} 為必填。") : null;
        if (text.Length > maxLength || text.Any(char.IsControl)) throw Rule(400, $"{field} 不可超過 {maxLength} 字元或含控制字元。");
        return text;
    }

    private static string? OptionalText(string? value, string field, int maxLength, bool multiline = false)
    {
        var text = (value ?? "").Trim();
        if (text.Length > maxLength || text.Any(ch => char.IsControl(ch) && !(multiline && ch is '\r' or '\n' or '\t')))
            throw Rule(400, $"{field} 不可超過 {maxLength} 字元或含非法控制字元。");
        return text.Length == 0 ? null : text;
    }

    private static void EnsureUnique(IEnumerable<string> values, string field)
    {
        var keys = values.Select(value => value.Trim()).ToArray();
        if (keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != keys.Length) throw Rule(400, $"{field} 不可重複。");
    }

    private static bool Same(string? left, string? right) => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string name)
    {
        using var command = new NpgsqlCommand("SELECT to_regclass(format('%I.%I','YMES',@name)) IS NOT NULL;", connection, transaction);
        Text(command, "name", name, 63);
        return command.ExecuteScalar() is true;
    }

    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction, string name)
    {
        if (!TableExists(connection, transaction, name)) throw Rule(503, $"YMES 尚未建立 {name.ToUpperInvariant()}，請先核對來源 DDL 並套用 migration 0089。");
    }

    private static void EnsureTables(NpgsqlConnection connection, NpgsqlTransaction? transaction, params string[] names)
    {
        foreach (var name in names) EnsureTable(connection, transaction, name);
    }

    private static void Text(NpgsqlCommand command, string name, string? value, int size) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = (object?)value ?? DBNull.Value;

    private static void Number(NpgsqlCommand command, string name, decimal? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;

    private static void Number(NpgsqlCommand command, string name, int? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;

    private static void Number(NpgsqlCommand command, string name, int value) => Number(command, name, (int?)value);

    private static int? NullableInt(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetInt32(index);
    private static string? NullableText(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
    private static DateTime? Date(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    private static Qc212RuleException Rule(int status, string message) => new(status, message);
    private static Qc212RuleException Conflict() => Rule(409, "資料已被其他作業修改，請重新查詢後再試。");

    private sealed record Qc212GroupWriteValue(
        string GroupNo, string GroupName, int LotIsValid, int ComponentCollect, string? PlanNo, string? FormName,
        string? WebUrl, int UnionErf, string? ErrorReportName, string? NoticeReportName, string? Description,
        int CursorMode, string? QcClass, string? GetFilePath, string? ExecutionFile);
}
