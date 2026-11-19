using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Operations;

public sealed class OpB02Repository(NpgsqlDataSource dataSource)
{
    private const decimal MaxStamp = 99_999_999_999m;
    private const string GroupColumns = "opgroupno,opgroupname,psno,opgrouporder::int,description,issuestate::int,creator,createdate,datastamp,reviser,revisedate";

    public IReadOnlyList<OpB02GroupDto> List(string? issueState, string? opGroupNo)
    {
        int? state = null;
        if (!string.IsNullOrWhiteSpace(issueState) && !string.Equals(issueState, "all", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(issueState, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value is not (-1 or 0 or 1 or 2))
                throw Rule(400, "IssueState 只允許 all、-1、0、1、2。");
            state = value;
        }
        var search = Search(opGroupNo);
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand($"SELECT {GroupColumns} FROM \"YMES\".tblopecngroupbasis WHERE (@state IS NULL OR issuestate=@state) AND (@search IS NULL OR position(lower(@search) in lower(opgroupno))>0) ORDER BY opgrouporder NULLS LAST,lower(btrim(opgroupno)) COLLATE \"C\",opgroupno COLLATE \"C\";", connection);
        Number(command, "state", state); Text(command, "search", search);
        using var reader = command.ExecuteReader(); var rows = new List<OpB02GroupDto>();
        while (reader.Read()) rows.Add(MapGroup(reader));
        return rows;
    }

    public OpB02GroupDto Get(string opGroupNo)
    {
        using var connection = dataSource.OpenConnection();
        return ReadGroup(connection, null, Key(opGroupNo, "OPGroupNo", 20), false);
    }

    public OpB02OperationsDto Operations(string opGroupNo)
    {
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var group = ReadGroup(connection, transaction, Key(opGroupNo, "OPGroupNo", 20), false, share: true);
        AssertChildOwners(connection, transaction, group.OpGroupNo);
        var rows = ReadOperations(connection, transaction, group.OpGroupNo);
        transaction.Commit(); return new(group, rows);
    }

    public OpB02GroupDto Create(OpB02GroupWriteRequest request, string actor)
    {
        actor = Actor(actor);
        var no = Key(request.OpGroupNo, "OPGroupNo", 20).ToUpperInvariant();
        var name = Name(request.OpGroupName); var description = Description(request.Description);
        ValidateOrder(request.OpGroupOrder, false);
        var ps = Key(request.PsNo, "PSNo", 50);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        Acquire(connection, transaction, "Group", no);
        using (var check = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblopecngroupbasis WHERE lower(btrim(opgroupno))=lower(btrim(@no))) OR EXISTS(SELECT 1 FROM \"YMES\".tblopecngroupdetail WHERE lower(btrim(opgroupno))=lower(btrim(@no)));", connection, transaction))
        {
            Text(check, "no", no);
            if (check.ExecuteScalar() is true) throw Rule(409, "群組編號已存在或含舊無主檔明細，不能接管來源資料。");
        }
        ps = ApprovedSegment(connection, transaction, ps);
        using var command = new NpgsqlCommand($"""
            INSERT INTO "YMES".tblopecngroupbasis
                (opgroupno,opgroupname,psno,opgrouporder,description,issuestate,creator,createdate,datastamp,reviser,revisedate)
            VALUES(@no,@name,@ps,@order,@description,0,@actor,now() AT TIME ZONE 'UTC',0,NULL,NULL)
            RETURNING {GroupColumns};
            """, connection, transaction);
        BindGroup(command, no, name, ps, request.OpGroupOrder, description, actor);
        var result = Returned(command); transaction.Commit(); return result;
    }

    public OpB02GroupDto Update(string opGroupNo, OpB02GroupUpdateRequest request, string actor)
    {
        actor = Actor(actor); ValidateStamp(request.DataStamp);
        var name = Name(request.OpGroupName); var description = Description(request.Description);
        ValidateOrder(request.OpGroupOrder, true);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var current = LockGroup(connection, transaction, Key(opGroupNo, "OPGroupNo", 20));
        Expected(current.DataStamp, request.DataStamp); RequireDraft(current); Incrementable(current.DataStamp);
        if (request.OpGroupNo is not null && !SameKey(connection, transaction, current.OpGroupNo, Key(request.OpGroupNo, "OPGroupNo", 20)))
            throw Rule(400, "修改時不可變更 OPGroupNo。");
        if (request.OpGroupOrder is null && current.OpGroupOrder is not null) throw Rule(400, "群組順序不可由有效值改為 NULL。");
        var ps = current.PsNo;
        if (!SameKey(connection, transaction, current.PsNo, request.PsNo))
        {
            ps = ApprovedSegment(connection, transaction, Key(request.PsNo, "PSNo", 50));
            EnsureMemberScope(connection, transaction, current.OpGroupNo, ps);
        }
        using var command = new NpgsqlCommand($"""
            UPDATE "YMES".tblopecngroupbasis
            SET opgroupname=@name,psno=@ps,opgrouporder=@order,description=@description,
                reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE opgroupno=@no AND datastamp IS NOT DISTINCT FROM @stamp AND issuestate=0
            RETURNING {GroupColumns};
            """, connection, transaction);
        BindGroup(command, current.OpGroupNo, name, ps, request.OpGroupOrder, description, actor);
        Number(command, "stamp", request.DataStamp);
        var result = Returned(command); transaction.Commit(); return result;
    }

    public OpB02DeleteResult Delete(string opGroupNo, OpB02ExpectedRequest request, string actor)
    {
        actor = Actor(actor); ValidateStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var current = LockGroup(connection, transaction, Key(opGroupNo, "OPGroupNo", 20));
        Expected(current.DataStamp, request.DataStamp);
        if (current.IssueState == 2)
        {
            Incrementable(current.DataStamp);
            var retired = ChangeState(connection, transaction, current, -1, actor);
            transaction.Commit(); return new(false, retired);
        }
        if (current.IssueState is not (0 or -1)) throw Rule(409, "簽核中或未知狀態不可刪除／停用。");
        using (var children = new NpgsqlCommand("DELETE FROM \"YMES\".tblopecngroupdetail WHERE opgroupno=@no;", connection, transaction))
        { Text(children, "no", current.OpGroupNo); children.ExecuteNonQuery(); }
        using (var command = new NpgsqlCommand("DELETE FROM \"YMES\".tblopecngroupbasis WHERE opgroupno=@no AND datastamp IS NOT DISTINCT FROM @stamp AND issuestate=@state;", connection, transaction))
        {
            Text(command, "no", current.OpGroupNo); Number(command, "stamp", current.DataStamp); Number(command, "state", current.IssueState);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
        }
        transaction.Commit(); return new(true, null);
    }

    public OpB02GroupDto Approve(string opGroupNo, OpB02ExpectedRequest request, string actor)
    {
        actor = Actor(actor); ValidateStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var current = LockGroup(connection, transaction, Key(opGroupNo, "OPGroupNo", 20));
        Expected(current.DataStamp, request.DataStamp); RequireDraft(current); Incrementable(current.DataStamp);
        // Explicit YMES local no-workflow policy, NOT the original Issue Job
        // engine. Catalog existence never establishes a Job's empty detail.
        if (WorkflowMayExist(connection, transaction))
            throw Rule(503, "本地存在 Issue／Job 來源；OP ECN GROUP BASIS 尚未接入正式簽核、回呼與通知服務，本次資料未變更。");
        var result = ChangeState(connection, transaction, current, 2, actor);
        transaction.Commit(); return result;
    }

    public OpB02OperationSavedDto AddOperation(string opGroupNo, OpB02OperationCreateRequest request, string actor)
    {
        actor = Actor(actor); ValidateStamp(request.DataStamp);
        var op = Key(request.OpNo, "OPNo", 20);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var group = LockGroup(connection, transaction, Key(opGroupNo, "OPGroupNo", 20));
        Expected(group.DataStamp, request.DataStamp); Incrementable(group.DataStamp);
        Acquire(connection, transaction, "OP", op);
        var operation = ReadOperation(connection, transaction, op, required: true)!;
        if (operation.IssueState != 2) throw Rule(422, "新增明細只允許已核准的真實作業站。");
        if (string.IsNullOrWhiteSpace(group.PsNo) || !ExactSame(connection, transaction, group.PsNo, operation.PsNo))
            throw Rule(422, "作業站與群組 PSNo 必須字面相等；* 不是全區段萬用值。");
        if (ReadGlobalDetail(connection, transaction, operation.No) is not null)
            throw Rule(409, "此作業站已屬於 ECN 群組；OPNo 為全表唯一主鍵。");
        using (var command = new NpgsqlCommand("INSERT INTO \"YMES\".tblopecngroupdetail(opgroupno,opno) VALUES(@group,@op);", connection, transaction))
        { Text(command, "group", group.OpGroupNo); Text(command, "op", operation.No); command.ExecuteNonQuery(); }
        var saved = BumpGroup(connection, transaction, group, actor);
        transaction.Commit(); return new(saved, new(group.OpGroupNo, operation.No, operation.Name));
    }

    public OpB02OperationDeletedDto DeleteOperation(string opGroupNo, string opNo, OpB02ExpectedRequest request, string actor)
    {
        actor = Actor(actor); ValidateStamp(request.DataStamp);
        var op = Key(opNo, "OPNo", 20);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var group = LockGroup(connection, transaction, Key(opGroupNo, "OPGroupNo", 20));
        Expected(group.DataStamp, request.DataStamp); Incrementable(group.DataStamp);
        Acquire(connection, transaction, "OP", op);
        // A member delete does not reselect or require a still-active OP master.
        var current = ReadGlobalDetail(connection, transaction, op) ?? throw Rule(404, "找不到指定 ECN 作業站明細。");
        if (!string.Equals(current.OpGroupNo, group.OpGroupNo, StringComparison.Ordinal))
            throw Rule(409, "此作業站的實際明細不屬於指定群組，原資料已保留。");
        using (var command = new NpgsqlCommand("DELETE FROM \"YMES\".tblopecngroupdetail WHERE opgroupno=@group AND opno=@op;", connection, transaction))
        {
            Text(command, "group", current.OpGroupNo); Text(command, "op", current.OpNo);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
        }
        var saved = BumpGroup(connection, transaction, group, actor);
        transaction.Commit(); return new(saved);
    }

    public IReadOnlyList<OpB02SegmentOption> Segments()
    {
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("SELECT psno,psname,issuestate::int,count(*) OVER(PARTITION BY lower(btrim(psno))) FROM \"YMES\".tblprsproductionsegment ORDER BY lower(btrim(psno)) COLLATE \"C\",psno COLLATE \"C\";", connection);
        using var reader = command.ExecuteReader(); var rows = new List<OpB02SegmentOption>();
        while (reader.Read())
        {
            if (Int(reader, 2) != 2) continue;
            if (reader.GetInt64(3) > 1) throw Rule(409, "生產區段存在大小寫／空白歧義。");
            rows.Add(new(reader.GetString(0), String(reader, 1), 2));
        }
        // No synthetic '*' row: ECN uses real PS literal equality.
        return rows;
    }

    public IReadOnlyList<OpB02OperationOption> OperationChoices(string psNo, int issueState, bool notInAnyGroup)
    {
        if (issueState != 2 || !notInAnyGroup) throw Rule(400, "明細查找只提供 IssueState=2、notInAnyGroup=true。");
        // PS here is a stored relationship VALUE, not a lookup identity to
        // normalize. Preserve legacy padding for the source's literal '='.
        _ = Key(psNo, "PSNo", 50);
        if (psNo.Length > 50) throw Rule(400, "PSNo 最多50字元。");
        var ps = psNo;
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("""
            WITH operations AS (
                SELECT opno,opname,psno,optype,opclass,issuestate,
                       count(*) OVER(PARTITION BY lower(btrim(opno))) AS aliases
                FROM "YMES".tblopbasis
            )
            SELECT o.opno,o.opname,o.psno,o.optype,o.opclass::int,o.issuestate::int,o.aliases
            FROM operations o
            WHERE o.issuestate=2 AND o.psno=@ps
              AND NOT EXISTS(SELECT 1 FROM "YMES".tblopecngroupdetail d WHERE lower(btrim(d.opno))=lower(btrim(o.opno)))
            ORDER BY lower(btrim(o.opno)) COLLATE "C",o.opno COLLATE "C";
            """, connection);
        Text(command, "ps", ps); using var reader = command.ExecuteReader(); var rows = new List<OpB02OperationOption>();
        while (reader.Read())
        {
            if (reader.GetInt64(6) > 1) throw Rule(409, "作業站主檔存在大小寫／空白歧義。");
            rows.Add(new(reader.GetString(0), reader.GetString(1), String(reader, 2), String(reader, 3), Int(reader, 4), reader.GetInt32(5)));
        }
        return rows;
    }

    private static void Acquire(NpgsqlConnection connection, NpgsqlTransaction transaction, string kind, string no)
    {
        using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:OPB02:' || @kind || ':' || lower(btrim(@no)),0));", connection, transaction);
        Text(command, "kind", kind); Text(command, "no", no); command.ExecuteNonQuery();
    }
    private static OpB02GroupDto LockGroup(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        Acquire(connection, transaction, "Group", no);
        var group = ReadGroup(connection, transaction, no, true);
        AssertChildOwners(connection, transaction, group.OpGroupNo); return group;
    }
    private static OpB02GroupDto ReadGroup(NpgsqlConnection connection, NpgsqlTransaction? transaction, string no, bool update, bool share = false)
    {
        using var command = new NpgsqlCommand($"SELECT {GroupColumns} FROM \"YMES\".tblopecngroupbasis WHERE lower(btrim(opgroupno))=lower(btrim(@no)) ORDER BY lower(btrim(opgroupno)) COLLATE \"C\",opgroupno COLLATE \"C\"" + (update ? " FOR UPDATE;" : share ? " FOR SHARE;" : ";"), connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到 ECN 作業站群組。");
        var result = MapGroup(reader);
        if (reader.Read()) throw Rule(409, "群組編號存在大小寫／前後空白歧義，資料已保留。");
        return result;
    }
    private static void AssertChildOwners(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblopecngroupdetail WHERE lower(btrim(opgroupno))=lower(btrim(@no)) AND opgroupno<>@no LIMIT 1;", connection, transaction);
        Text(command, "no", no);
        if (command.ExecuteScalar() is not null) throw Rule(409, "既有明細的群組編號含大小寫／空白別名，不能忽略、接管或重寫。");
    }
    private static IReadOnlyList<OpB02OperationDto> ReadOperations(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("""
            SELECT d.opgroupno,d.opno,o.opname,count(o.opno) OVER(PARTITION BY d.opno),
                   (SELECT count(*) FROM "YMES".tblopecngroupdetail x WHERE lower(btrim(x.opno))=lower(btrim(d.opno)))
            FROM "YMES".tblopecngroupdetail d
            LEFT JOIN "YMES".tblopbasis o ON lower(btrim(o.opno))=lower(btrim(d.opno))
            WHERE d.opgroupno=@no ORDER BY lower(btrim(d.opno)) COLLATE "C",d.opno COLLATE "C";
            """, connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader(); var rows = new List<OpB02OperationDto>();
        while (reader.Read())
        {
            if (reader.GetInt64(3) > 1 || reader.GetInt64(4) > 1) throw Rule(409, "作業站或全域 ECN 明細存在大小寫／空白歧義，資料已保留。");
            rows.Add(new(reader.GetString(0), reader.GetString(1), String(reader, 2)));
        }
        return rows;
    }
    private static OpB02OperationDto? ReadGlobalDetail(NpgsqlConnection connection, NpgsqlTransaction transaction, string op)
    {
        using var command = new NpgsqlCommand("SELECT opgroupno,opno FROM \"YMES\".tblopecngroupdetail WHERE lower(btrim(opno))=lower(btrim(@op)) ORDER BY opno COLLATE \"C\" FOR UPDATE;", connection, transaction);
        Text(command, "op", op); using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var row = new OpB02OperationDto(reader.GetString(0), reader.GetString(1), null);
        if (reader.Read()) throw Rule(409, "OPNo 全域明細存在大小寫／空白歧義，不能任選一筆。");
        return row;
    }
    private sealed record Operation(string No, string Name, string? PsNo, int? IssueState);
    private static Operation? ReadOperation(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, bool required)
    {
        using var command = new NpgsqlCommand("SELECT opno,opname,psno,issuestate::int FROM \"YMES\".tblopbasis WHERE lower(btrim(opno))=lower(btrim(@no)) ORDER BY opno COLLATE \"C\" FOR SHARE;", connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) return required ? throw Rule(422, "作業站不存在。") : null;
        var row = new Operation(reader.GetString(0), reader.GetString(1), String(reader, 2), Int(reader, 3));
        if (reader.Read()) throw Rule(409, "作業站主檔存在大小寫／空白歧義。");
        return row;
    }
    private static string ApprovedSegment(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT psno,issuestate::int FROM \"YMES\".tblprsproductionsegment WHERE lower(btrim(psno))=lower(btrim(@no)) ORDER BY psno COLLATE \"C\" FOR SHARE;", connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(422, "生產區段不存在。");
        var ps = reader.GetString(0); var state = Int(reader, 1);
        if (reader.Read()) throw Rule(409, "生產區段存在大小寫／空白歧義。");
        // Explicit conservative YMES policy; source LoadPSDefine state filter
        // is not delivered. '*' is validated as a real PS, never synthesized.
        if (state != 2) throw Rule(422, "新建或變更群組只能選已核准的真實生產區段。");
        return ps;
    }
    private static void EnsureMemberScope(NpgsqlConnection connection, NpgsqlTransaction transaction, string group, string ps)
    {
        var members = ReadOperations(connection, transaction, group);
        var names = new List<string>();
        using (var order = new NpgsqlCommand("SELECT DISTINCT lower(btrim(value)) COLLATE \"C\" AS no FROM unnest(@names) AS input(value) ORDER BY no;", connection, transaction))
        {
            order.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = members.Select(row => row.OpNo).ToArray();
            using var reader = order.ExecuteReader(); while (reader.Read()) names.Add(reader.GetString(0));
        }
        foreach (var no in names)
        {
            var op = ReadOperation(connection, transaction, no, required: false);
            if (op is null || !ExactSame(connection, transaction, op.PsNo, ps))
                throw Rule(409, "既有成員無法確認屬於新 PSNo；群組區段未變更。* 也必須字面相等。");
        }
    }
    private static bool WorkflowMayExist(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        using var command = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c
              JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname NOT IN ('pg_catalog','information_schema')
                AND c.relkind IN ('r','p','v','m','f')
                AND (lower(c.relname)=ANY(@known)
                  OR lower(c.relname) LIKE 'tblisu%'
                  OR lower(c.relname) LIKE '%issue%job%'
                  OR lower(c.relname) LIKE '%job%issue%'));
            """, connection, transaction);
        command.Parameters.Add("known", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" };
        return command.ExecuteScalar() is true;
    }
    private static OpB02GroupDto ChangeState(NpgsqlConnection connection, NpgsqlTransaction transaction, OpB02GroupDto current, int state, string actor)
    {
        using var command = new NpgsqlCommand($"UPDATE \"YMES\".tblopecngroupbasis SET issuestate=@next,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1 WHERE opgroupno=@no AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {GroupColumns};", connection, transaction);
        Text(command, "no", current.OpGroupNo); Text(command, "actor", actor); Number(command, "state", current.IssueState);
        Number(command, "next", state); Number(command, "stamp", current.DataStamp); return Returned(command);
    }
    private static OpB02GroupDto BumpGroup(NpgsqlConnection connection, NpgsqlTransaction transaction, OpB02GroupDto current, string actor)
    {
        using var command = new NpgsqlCommand($"UPDATE \"YMES\".tblopecngroupbasis SET reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1 WHERE opgroupno=@no AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {GroupColumns};", connection, transaction);
        Text(command, "no", current.OpGroupNo); Text(command, "actor", actor); Number(command, "stamp", current.DataStamp); return Returned(command);
    }
    private static bool SameKey(NpgsqlConnection connection, NpgsqlTransaction transaction, string? left, string? right)
    {
        using var command = new NpgsqlCommand("SELECT lower(btrim(@left)) IS NOT DISTINCT FROM lower(btrim(@right));", connection, transaction);
        Text(command, "left", left); Text(command, "right", right); return command.ExecuteScalar() is true;
    }
    private static bool ExactSame(NpgsqlConnection connection, NpgsqlTransaction transaction, string? left, string? right)
    {
        using var command = new NpgsqlCommand("SELECT @left IS NOT DISTINCT FROM @right;", connection, transaction);
        Text(command, "left", left); Text(command, "right", right); return command.ExecuteScalar() is true;
    }
    private static void RequireDraft(OpB02GroupDto row) { if (row.IssueState != 0) throw Rule(409, "只有未簽核狀態 0 可修改或核准。"); }
    private static void Expected(decimal? actual, decimal? expected) { if (actual != expected) throw Conflict(); }
    private static void Incrementable(decimal? stamp) { ValidateStamp(stamp); if (stamp == MaxStamp) throw Rule(409, "DataStamp 已達來源上限，資料未變更。"); }
    private static void ValidateStamp(decimal? stamp)
    { if (stamp is { } value && (value != decimal.Truncate(value) || value < -MaxStamp || value > MaxStamp)) throw Rule(400, "DataStamp 必須明確為 NULL 或來源 numeric(11,0) 範圍內整數。"); }
    private static void ValidateOrder(int? value, bool nullable)
    { if ((!nullable && value is null) || value is < 0 or > 999999) throw Rule(400, "群組順序必須為 0–999999 整數。"); }
    private static string Key(string? value, string label, int length)
    { var text = value?.Trim(' ') ?? ""; if (text.Length is < 1 || text.Length > length || text.Any(char.IsControl)) throw Rule(400, $"{label} 必填、最多 {length} 字元且不可含控制字元。"); return text; }
    private static string? Search(string? value)
    { var text = value?.Trim(' '); if (text?.Length > 20 || text?.Any(char.IsControl) == true) throw Rule(400, "查詢編號最多20字元且不可含控制字元。"); return string.IsNullOrEmpty(text) ? null : text; }
    private static string Name(string? value)
    { if (string.IsNullOrWhiteSpace(value) || value.Length > 50 || value.Any(char.IsControl)) throw Rule(400, "群組名稱必填、最多50字元且不可含控制字元。"); return value; }
    private static string? Description(string? value)
    { if (value?.Length > 255 || value?.Any(c => char.IsControl(c) && c is not '\r' and not '\n' and not '\t') == true) throw Rule(400, "說明最多255字元且不可含無效控制字元。"); return string.IsNullOrEmpty(value) ? null : value; }
    private static string Actor(string value) => Key(value, "登入使用者", 10);
    private static string? String(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static int? Int(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    private static DateTime? Date(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc);
    private static OpB02GroupDto MapGroup(NpgsqlDataReader reader) => new(reader.GetString(0), reader.GetString(1), String(reader, 2), Int(reader, 3), String(reader, 4), reader.GetInt32(5), String(reader, 6), Date(reader, 7), reader.IsDBNull(8) ? null : reader.GetDecimal(8), String(reader, 9), Date(reader, 10));
    private static OpB02GroupDto Returned(NpgsqlCommand command)
    { using var reader = command.ExecuteReader(); if (!reader.Read()) throw Conflict(); var row = MapGroup(reader); if (reader.Read()) throw Conflict(); return row; }
    private static void BindGroup(NpgsqlCommand command, string no, string name, string? ps, int? order, string? description, string actor)
    { Text(command, "no", no); Text(command, "name", name); Text(command, "ps", ps); Number(command, "order", order); Text(command, "description", description); Text(command, "actor", actor); }
    private static void Text(NpgsqlCommand command, string name, string? value) => command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand command, string name, decimal? value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static OpB02RuleException Rule(int status, string message) => new(status, message);
    private static OpB02RuleException Conflict() => Rule(409, "群組或明細已被其他操作更新，請重新查詢；本次未保存。");
}
