using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc213Repository(NpgsqlDataSource dataSource)
{
    private const int MaxDetails = 1000;

    public IReadOnlyList<Qc213GroupDto> ListGroups(string? functionNo, int? issueState, string? groupNo)
    {
        if (!string.Equals(functionNo, "QCITEM_G_A_ORDER", StringComparison.Ordinal))
            throw Rule(400, "FunctionNo 必須為 QCITEM_G_A_ORDER。");
        ValidateIssueState(issueState);
        var search = OptionalText(groupNo, "GroupNo 查詢", 25) ?? "";
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcitemattgrouporderbasis");
        using var command = new NpgsqlCommand("""
            SELECT groupno,groupname,COALESCE(lotisvalid,0)::int,description,COALESCE(issuestate,0)::int,
                   creator,createdate,reviser,revisedate,COALESCE(datastamp,0)
            FROM "YMES".tblqcitemattgrouporderbasis
            WHERE (@state IS NULL OR COALESCE(issuestate,0)=@state)
              AND (@search='' OR position(lower(@search) in lower(groupno))>0)
            ORDER BY groupno LIMIT 2000;
            """, connection);
        Number(command, "state", issueState);
        Text(command, "search", search, 25);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc213GroupDto>();
        while (reader.Read()) rows.Add(ReadGroup(reader));
        return rows;
    }

    public IReadOnlyList<Qc213DetailDto> Details(string groupNo)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        EnsureTables(connection, transaction, "tblqcitemattgrouporderbasis", "tblqcitemattgorderdetail", "tblqcitemforattributes");
        var group = ReadGroup(connection, transaction, no, false);
        using var command = new NpgsqlCommand("""
            SELECT d.groupno,d.qcitemno,COALESCE(i.qcitemname,''),COALESCE(d.showorder,0)::int,
                   (SELECT count(*) FROM "YMES".tblqcitemforattributes dup
                    WHERE lower(btrim(dup.qcitemno))=lower(btrim(d.qcitemno)))
            FROM "YMES".tblqcitemattgorderdetail d
            JOIN "YMES".tblqcitemforattributes i
              ON lower(btrim(i.qcitemno))=lower(btrim(d.qcitemno))
            WHERE lower(btrim(d.groupno))=lower(btrim(@no))
            ORDER BY COALESCE(d.showorder,0),d.qcitemno LIMIT 1000;
            """, connection, transaction);
        Text(command, "no", group.GroupNo, 25);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc213DetailDto>();
        while (reader.Read())
        {
            if (reader.GetInt64(4) != 1) throw Rule(409, "計數品管項目編號在 YMES 存在重複或歧義，明細未完整載入。");
            rows.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3)));
        }
        return rows;
    }

    public Qc213GroupDto Create(Qc213GroupWriteRequest request, string actor)
    {
        var value = Normalize(request.GroupNo, request.GroupName, request.LotIsValid, request.Description);
        actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTables(connection, transaction, "tblqcitemattgrouporderbasis", "tblqcitemgroupbasis", "tblqcitemgroupbasis_pmi", "tblqcitemforattributes", "tblqcitemforvariables");
        QcItemIdentityGuard.Acquire(connection, transaction, [value.GroupNo]);
        QcItemIdentityGuard.EnsureAvailable(connection, transaction, value.GroupNo);
        EnsureGroupCodeAvailable(connection, transaction, value.GroupNo);
        using var command = new NpgsqlCommand("""
            INSERT INTO "YMES".tblqcitemattgrouporderbasis
                (groupno,groupname,lotisvalid,issuestate,description,creator,createdate,datastamp)
            VALUES (@no,@name,@lot,0,@description,@actor,now() AT TIME ZONE 'UTC',0)
            RETURNING groupno,groupname,COALESCE(lotisvalid,0)::int,description,COALESCE(issuestate,0)::int,
                      creator,createdate,reviser,revisedate,COALESCE(datastamp,0);
            """, connection, transaction);
        Text(command, "no", value.GroupNo, 25); Text(command, "name", value.GroupName, 50);
        Number(command, "lot", value.LotIsValid); Text(command, "description", value.Description, 255); Text(command, "actor", actor, 10);
        var result = ReadReturnedGroup(command);
        transaction.Commit();
        return result;
    }

    public Qc213GroupDto Update(string groupNo, Qc213GroupUpdateRequest request, string actor)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!.ToUpperInvariant();
        if (!Same(request.GroupNo, no)) throw Rule(400, "路徑與表單中的 GroupNo 不一致。");
        var value = Normalize(no, request.GroupName, request.LotIsValid, request.Description);
        var expectedStamp = ExpectedStamp(request.DataStamp);
        actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTable(connection, transaction, "tblqcitemattgrouporderbasis");
        var current = ReadGroup(connection, transaction, no, true);
        ExpectDraft(current, expectedStamp);
        using var command = new NpgsqlCommand("""
            UPDATE "YMES".tblqcitemattgrouporderbasis
               SET groupname=@name,lotisvalid=@lot,description=@description,
                   datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC'
             WHERE groupno=@no AND COALESCE(issuestate,0)=0 AND COALESCE(datastamp,0)=@stamp
            RETURNING groupno,groupname,COALESCE(lotisvalid,0)::int,description,COALESCE(issuestate,0)::int,
                      creator,createdate,reviser,revisedate,COALESCE(datastamp,0);
            """, connection, transaction);
        Text(command, "no", current.GroupNo, 25); Text(command, "name", value.GroupName, 50);
        Number(command, "lot", value.LotIsValid); Text(command, "description", value.Description, 255);
        Text(command, "actor", actor, 10); Number(command, "stamp", expectedStamp);
        var result = ReadReturnedGroup(command);
        transaction.Commit();
        return result;
    }

    public void Delete(string groupNo, Qc213CommandRequest request, string actor)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        var expectedStamp = ExpectedStamp(request.DataStamp);
        actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTable(connection, transaction, "tblqcitemattgrouporderbasis");
        var current = ReadGroup(connection, transaction, no, true);
        ExpectStamp(current, expectedStamp);
        if (current.IssueState == 1) throw Rule(409, "簽核中的群組不可刪除。");
        if (current.IssueState == 2)
        {
            using var retire = new NpgsqlCommand("""
                UPDATE "YMES".tblqcitemattgrouporderbasis
                   SET issuestate=-1,datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC'
                 WHERE groupno=@no AND issuestate=2 AND COALESCE(datastamp,0)=@stamp;
                """, connection, transaction);
            Text(retire, "no", current.GroupNo, 25); Text(retire, "actor", actor, 10); Number(retire, "stamp", expectedStamp);
            if (retire.ExecuteNonQuery() != 1) throw Conflict();
            transaction.Commit();
            return;
        }
        if (current.IssueState is not (0 or -1)) throw Rule(409, "目前狀態不可刪除。");

        EnsureTables(connection, transaction, "tblqcitemattgorderdetail", "tblqcitemparameter", "tblopqcitem",
            "tblopgqcitemdetail", "tblprdqcitem", "tblwiptemp_qcitem", "tblqcitemgroupbasis",
            "tblqcitemgroupbasis_pmi", "tblqcitemforattributes", "tblqcitemforvariables");
        QcItemIdentityGuard.Acquire(connection, transaction, [current.GroupNo]);
        EnsureGroupIdentityExclusive(connection, transaction, current.GroupNo);
        using (var usage = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblwiptemp_qcitem WHERE lower(btrim(qcitemno))=lower(btrim(@no)) AND qcitemtype=3);", connection, transaction))
        {
            Text(usage, "no", current.GroupNo, 25);
            if (usage.ExecuteScalar() is true) throw Rule(409, "此群組仍被 WIP 暫存檢驗資料使用，未刪除。");
        }

        ExecuteDelete(connection, transaction, "DELETE FROM \"YMES\".tblqcitemattgorderdetail WHERE lower(btrim(groupno))=lower(btrim(@no));", current.GroupNo);
        ExecuteDelete(connection, transaction, "DELETE FROM \"YMES\".tblqcitemparameter WHERE lower(btrim(qcitemno))=lower(btrim(@no));", current.GroupNo);
        ExecuteDelete(connection, transaction, "DELETE FROM \"YMES\".tblopqcitem WHERE lower(btrim(qcitemno))=lower(btrim(@no)) AND qcitemtype=3;", current.GroupNo);
        ExecuteDelete(connection, transaction, "DELETE FROM \"YMES\".tblopgqcitemdetail WHERE lower(btrim(groupno))=lower(btrim(@no)) AND qcitemtype=3;", current.GroupNo);
        ExecuteDelete(connection, transaction, "DELETE FROM \"YMES\".tblprdqcitem WHERE lower(btrim(qcitemno))=lower(btrim(@no)) AND qcitemtype=3;", current.GroupNo);
        using (var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcitemattgrouporderbasis WHERE groupno=@no AND COALESCE(issuestate,0)=@state AND COALESCE(datastamp,0)=@stamp;", connection, transaction))
        {
            Text(delete, "no", current.GroupNo, 25); Number(delete, "state", current.IssueState); Number(delete, "stamp", expectedStamp);
            if (delete.ExecuteNonQuery() != 1) throw Conflict();
        }
        transaction.Commit();
    }

    public Qc213GroupDto Approve(string groupNo, Qc213CommandRequest request, string actor)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        var expectedStamp = ExpectedStamp(request.DataStamp);
        actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTables(connection, transaction, "tblqcitemattgrouporderbasis", "tblqcitemattgorderdetail", "tblqcitemforattributes");
        var current = ReadGroup(connection, transaction, no, true);
        ExpectDraft(current, expectedStamp);
        using (var details = new NpgsqlCommand("""
            SELECT EXISTS(
                SELECT 1 FROM "YMES".tblqcitemattgorderdetail d
                JOIN "YMES".tblqcitemforattributes i
                  ON lower(btrim(i.qcitemno))=lower(btrim(d.qcitemno))
                WHERE lower(btrim(d.groupno))=lower(btrim(@no)) AND COALESCE(i.issuestate,0)=2);
            """, connection, transaction))
        {
            Text(details, "no", current.GroupNo, 25);
            if (details.ExecuteScalar() is not true) throw Rule(422, "至少要有一筆已核准的 Attribute 品管項目才能核准。");
        }
        if (IssueTablesExist(connection, transaction))
            throw Rule(503, "YMES 已有 Issue/Job 表，但 QC213 簽核尚未接入共用流程；資料未變更。");
        using var command = new NpgsqlCommand("""
            UPDATE "YMES".tblqcitemattgrouporderbasis
               SET issuestate=2,datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC'
             WHERE groupno=@no AND COALESCE(issuestate,0)=0 AND COALESCE(datastamp,0)=@stamp
            RETURNING groupno,groupname,COALESCE(lotisvalid,0)::int,description,COALESCE(issuestate,0)::int,
                      creator,createdate,reviser,revisedate,COALESCE(datastamp,0);
            """, connection, transaction);
        Text(command, "no", current.GroupNo, 25); Text(command, "actor", actor, 10); Number(command, "stamp", expectedStamp);
        var result = ReadReturnedGroup(command);
        transaction.Commit();
        return result;
    }

    public void ReplaceDetails(string groupNo, Qc213DetailReplaceRequest request)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!;
        var rows = NormalizeDetails(request.Items);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTables(connection, transaction, "tblqcitemattgrouporderbasis", "tblqcitemattgorderdetail", "tblqcitemforattributes", "tblqcitemforvariables");
        var group = ReadGroup(connection, transaction, no, true);
        if (group.IssueState != 0) throw Rule(409, "只有未簽核群組可以設定品管項目。");

        var itemNos = rows.Select(row => row.QcItemNo).ToArray();
        if (itemNos.Length > 0)
        {
            QcItemIdentityGuard.Acquire(connection, transaction, itemNos);
            var keys = itemNos.Select(value => value.Trim()).ToArray();
            using var command = new NpgsqlCommand("""
                SELECT qcitemno,COALESCE(issuestate,0)::int
                FROM "YMES".tblqcitemforattributes
                WHERE lower(btrim(qcitemno))=ANY(@nos)
                ORDER BY lower(btrim(qcitemno)),qcitemno;
                """, connection, transaction);
            command.Parameters.Add("nos", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = keys.Select(value => value.ToLowerInvariant()).ToArray();
            using var reader = command.ExecuteReader();
            var found = new Dictionary<string, (string Canonical, int State)>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                var key = reader.GetString(0).Trim();
                if (!found.TryAdd(key, (reader.GetString(0), reader.GetInt32(1))))
                    throw Rule(409, "Attribute 品管項目編號在 YMES 存在大小寫／空白歧義。");
            }
            reader.Close();
            foreach (var row in rows)
            {
                QcItemIdentityGuard.EnsureAttributeOwner(connection, transaction, row.QcItemNo);
                if (!found.TryGetValue(row.QcItemNo, out var item)) throw Rule(422, $"找不到 Attribute 品管項目 {row.QcItemNo}。");
                if (item.State != 2) throw Rule(422, $"Attribute 品管項目 {row.QcItemNo} 尚未核准。");
                row.QcItemNo = item.Canonical;
            }
        }

        ExecuteDelete(connection, transaction, "DELETE FROM \"YMES\".tblqcitemattgorderdetail WHERE lower(btrim(groupno))=lower(btrim(@no));", group.GroupNo);
        for (var index = 0; index < rows.Count; index++)
        {
            using var insert = new NpgsqlCommand("INSERT INTO \"YMES\".tblqcitemattgorderdetail(groupno,qcitemno,showorder) VALUES(@group,@item,@order);", connection, transaction);
            Text(insert, "group", group.GroupNo, 25); Text(insert, "item", rows[index].QcItemNo, 25); Number(insert, "order", index);
            insert.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public IReadOnlyList<Qc213OperationDto> Operations(int? qcItemType, string? qcItemNo)
    {
        var groupNo = RequireTypeAndGroup(qcItemType, qcItemNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        EnsureTables(connection, transaction, "tblqcitemattgrouporderbasis", "tblopqcitem", "tblopbasis");
        var group = ReadGroup(connection, transaction, groupNo, false);
        EnsureNoOtherOperationType(connection, transaction, group.GroupNo);
        using var command = new NpgsqlCommand("""
            SELECT o.opno,
                   (SELECT min(b.opname) FROM "YMES".tblopbasis b WHERE lower(btrim(b.opno))=lower(btrim(o.opno))),
                   COALESCE(o.inline,0)::int,COALESCE(o.insptype_fai,0)::int,
                   COALESCE(o.insptype_ipqc,0)::int,COALESCE(o.insptype_pqc,0)::int,
                   COALESCE(o.executeorder,0)::int,
                   (SELECT count(*) FROM "YMES".tblopbasis b WHERE lower(btrim(b.opno))=lower(btrim(o.opno)))
            FROM "YMES".tblopqcitem o
            WHERE lower(btrim(o.qcitemno))=lower(btrim(@no)) AND o.qcitemtype=3
            ORDER BY COALESCE(o.executeorder,0),o.opno LIMIT 500;
            """, connection, transaction);
        Text(command, "no", group.GroupNo, 25);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc213OperationDto>();
        while (reader.Read())
        {
            if (reader.GetInt64(7) != 1) throw Rule(409, "作業站主檔不存在或 OPNo 有大小寫／空白歧義。");
            rows.Add(new(reader.GetString(0), NullableText(reader, 1), reader.GetInt32(2),
                reader.GetInt32(3) == 1, reader.GetInt32(4) == 1, reader.GetInt32(5) == 1, reader.GetInt32(6)));
        }
        return rows;
    }

    public void ReplaceOperations(int? qcItemType, string? qcItemNo, Qc213OperationReplaceRequest request, string actor)
    {
        var groupNo = RequireTypeAndGroup(qcItemType, qcItemNo);
        var rows = NormalizeOperations(request.Operations);
        actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        EnsureTables(connection, transaction, "tblqcitemattgrouporderbasis", "tblopqcitem", "tblopbasis");
        var group = ReadGroup(connection, transaction, groupNo, true);
        if (group.IssueState != 2) throw Rule(409, "只有已核准群組可以維護作業站對應。");
        if (group.LotIsValid == 0 && rows.Any(row => row.Inline)) throw Rule(422, "Lot Valid Check=否時不可指定 InLine 作業站。");
        if (rows.Any(row => row.Inline && !row.InspTypeFai && !row.InspTypeIpqc && !row.InspTypePqc))
            throw Rule(422, "每個 InLine 作業站至少要勾選一種 FAI、IPQC 或 PQC 檢驗時機。");
        EnsureNoOtherOperationType(connection, transaction, group.GroupNo);

        if (rows.Count > 0)
        {
            var opKeys = rows.Select(row => row.OpNo.Trim()).ToArray();
            using var command = new NpgsqlCommand("""
                SELECT opno,COALESCE(issuestate,0)::int
                FROM "YMES".tblopbasis
                WHERE lower(btrim(opno))=ANY(@nos)
                ORDER BY lower(btrim(opno)),opno;
                """, connection, transaction);
            command.Parameters.Add("nos", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = opKeys.Select(value => value.ToLowerInvariant()).ToArray();
            using var reader = command.ExecuteReader();
            var found = new Dictionary<string, (string Canonical, int State)>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                var key = reader.GetString(0).Trim();
                if (!found.TryAdd(key, (reader.GetString(0), reader.GetInt32(1))))
                    throw Rule(409, "作業站編號在 YMES 存在大小寫／空白歧義。");
            }
            reader.Close();
            foreach (var row in rows)
            {
                if (!found.TryGetValue(row.OpNo, out var op)) throw Rule(422, $"作業站 {row.OpNo} 不存在或尚未核准。");
                if (op.State != 2) throw Rule(422, $"作業站 {row.OpNo} 尚未核准。");
                row.OpNo = op.Canonical;
            }
        }

        ExecuteDelete(connection, transaction, "DELETE FROM \"YMES\".tblopqcitem WHERE lower(btrim(qcitemno))=lower(btrim(@no)) AND qcitemtype=3;", group.GroupNo);
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            using var insert = new NpgsqlCommand("""
                INSERT INTO "YMES".tblopqcitem
                    (opno,qcitemno,qcitemtype,inline,executeorder,insptype_fai,insptype_ipqc,insptype_pqc,reviser,revisedate)
                VALUES (@op,@no,3,@inline,@order,@fai,@ipqc,@pqc,@actor,now() AT TIME ZONE 'UTC');
                """, connection, transaction);
            Text(insert, "op", row.OpNo, 20); Text(insert, "no", group.GroupNo, 25);
            Number(insert, "inline", row.Inline ? 1 : 0); Number(insert, "order", index);
            Number(insert, "fai", row.InspTypeFai ? 1 : 0); Number(insert, "ipqc", row.InspTypeIpqc ? 1 : 0);
            Number(insert, "pqc", row.InspTypePqc ? 1 : 0); Text(insert, "actor", actor, 10);
            insert.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    private static string RequireTypeAndGroup(int? qcItemType, string? qcItemNo)
    {
        if (qcItemType != 3) throw Rule(400, "此作業站資源只允許 QCItemType=3。");
        return Code(qcItemNo, "QCItemNo", 25, true)!;
    }

    private static void EnsureGroupCodeAvailable(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("""
            SELECT EXISTS(
                SELECT 1 FROM "YMES".tblqcitemattgrouporderbasis WHERE lower(btrim(groupno))=lower(btrim(@no))
                UNION ALL
                SELECT 1 FROM "YMES".tblqcitemgroupbasis WHERE lower(btrim(groupno))=lower(btrim(@no))
                UNION ALL
                SELECT 1 FROM "YMES".tblqcitemgroupbasis_pmi WHERE lower(btrim(groupno))=lower(btrim(@no)));
            """, connection, transaction);
        Text(command, "no", no, 25);
        if (command.ExecuteScalar() is true) throw Rule(409, "GroupNo 已存在於計數、計量或 PMI 品管群組。");
    }

    private static void EnsureGroupIdentityExclusive(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("""
            SELECT EXISTS(
                SELECT 1 FROM "YMES".tblqcitemforattributes WHERE lower(btrim(qcitemno))=lower(btrim(@no))
                UNION ALL
                SELECT 1 FROM "YMES".tblqcitemforvariables WHERE lower(btrim(qcitemno))=lower(btrim(@no))
                UNION ALL
                SELECT 1 FROM "YMES".tblqcitemgroupbasis WHERE lower(btrim(groupno))=lower(btrim(@no))
                UNION ALL
                SELECT 1 FROM "YMES".tblqcitemgroupbasis_pmi WHERE lower(btrim(groupno))=lower(btrim(@no)));
            """, connection, transaction);
        Text(command, "no", no, 25);
        if (command.ExecuteScalar() is true) throw Rule(409, "同編號 QC Item 或其他品管群組已存在；為保護共用關聯資料，無法硬刪除這個群組。");
    }

    private static void EnsureNoOtherOperationType(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblopqcitem WHERE lower(btrim(qcitemno))=lower(btrim(@no)) AND qcitemtype<>3);", connection, transaction);
        Text(command, "no", no, 25);
        if (command.ExecuteScalar() is true) throw Rule(409, "同編號已有其他 QCItemType 的作業站對應；未覆寫共用資料。");
    }

    private static bool IssueTablesExist(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM unnest(@names::text[]) AS names(name) WHERE to_regclass(format('%I.%I','YMES',name)) IS NOT NULL);", connection, transaction);
        command.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" };
        return command.ExecuteScalar() is true;
    }

    private static void ExecuteDelete(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql, string no)
    {
        using var command = new NpgsqlCommand(sql, connection, transaction);
        Text(command, "no", no, 50);
        command.ExecuteNonQuery();
    }

    private static Qc213GroupDto ReadGroup(NpgsqlConnection connection, NpgsqlTransaction? transaction, string no, bool forUpdate)
    {
        using var command = new NpgsqlCommand("""
            SELECT groupno,groupname,COALESCE(lotisvalid,0)::int,description,COALESCE(issuestate,0)::int,
                   creator,createdate,reviser,revisedate,COALESCE(datastamp,0)
            FROM "YMES".tblqcitemattgrouporderbasis
            WHERE lower(btrim(groupno))=lower(btrim(@no))
            ORDER BY groupno
            """ + (forUpdate ? " FOR UPDATE;" : ";"), connection, transaction);
        Text(command, "no", no, 25);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到計數品管項目群組，請重新查詢。");
        var result = ReadGroup(reader);
        if (reader.Read()) throw Rule(409, "GroupNo 存在大小寫／空白歧義，請先整理 YMES 主檔。");
        return result;
    }

    private static Qc213GroupDto ReadGroup(NpgsqlDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), reader.GetInt32(2), NullableText(reader, 3), reader.GetInt32(4),
        NullableText(reader, 5), NullableDate(reader, 6), NullableText(reader, 7), NullableDate(reader, 8), reader.GetDecimal(9));

    private static Qc213GroupDto ReadReturnedGroup(NpgsqlCommand command)
    {
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Conflict();
        var result = ReadGroup(reader);
        if (reader.Read()) throw Rule(409, "GroupNo 存在重複資料，請先整理 YMES 主檔。");
        return result;
    }

    private static Qc213GroupValue Normalize(string? groupNo, string? groupName, int? lotIsValid, string? description)
    {
        var no = Code(groupNo, "GroupNo", 25, true)!.ToUpperInvariant();
        var name = Code(groupName, "GroupName", 50, true)!;
        if (lotIsValid is not (0 or 1)) throw Rule(400, "LotIsValid 必須為 0 或 1。");
        return new(no, name, lotIsValid.Value, OptionalText(description, "Description", 255, true));
    }

    private static List<Qc213DetailWriteRequest> NormalizeDetails(List<Qc213DetailWriteRequest>? input)
    {
        var rows = input ?? throw Rule(400, "品管項目明細清單不可為 NULL。");
        if (rows.Count > MaxDetails) throw Rule(413, $"每個群組最多可設定 {MaxDetails} 筆明細。");
        var normalized = rows.Select(row => new Qc213DetailWriteRequest
        {
            QcItemNo = Code(row.QcItemNo, "QCItemNo", 25, true)!, ShowOrder = row.ShowOrder
        }).ToList();
        EnsureUnique(normalized.Select(row => row.QcItemNo), "QCItemNo");
        var orders = normalized.Select(row => row.ShowOrder).ToArray();
        if (orders.Any(value => value is null or < 0 or > 999) || orders.Distinct().Count() != orders.Length ||
            orders.Order().Where((value, index) => value != index).Any())
            throw Rule(400, "ShowOrder 必須由 0 開始且連續，最多 1000 筆。");
        return normalized.OrderBy(row => row.ShowOrder).ToList();
    }

    private static List<Qc213OperationWriteRequest> NormalizeOperations(List<Qc213OperationWriteRequest>? input)
    {
        var rows = input ?? throw Rule(400, "作業站清單不可為 NULL。");
        if (rows.Count > 100) throw Rule(413, "QC213 最多可對應 100 個作業站。");
        foreach (var row in rows) row.OpNo = Code(row.OpNo, "OPNo", 20, true)!;
        EnsureUnique(rows.Select(row => row.OpNo), "OPNo");
        return rows;
    }

    private static void ExpectDraft(Qc213GroupDto group, decimal expectedStamp)
    {
        if (group.IssueState != 0) throw Rule(409, "只有未簽核群組可以修改或核准。");
        ExpectStamp(group, expectedStamp);
    }

    private static void ExpectStamp(Qc213GroupDto group, decimal expectedStamp)
    {
        if (group.DataStamp != expectedStamp) throw Conflict();
        if (group.DataStamp >= 99999999999m) throw Rule(409, "DataStamp 已達欄位上限，請先處理主檔版本資料。");
    }

    private static decimal ExpectedStamp(decimal? value)
    {
        if (!value.HasValue || decimal.Truncate(value.Value) != value.Value || value.Value is < 0 or > 99999999999m)
            throw Rule(400, "必須提供 11 位數範圍內的整數 DataStamp。");
        return value.Value;
    }

    private static void ValidateIssueState(int? issueState)
    {
        if (issueState is not null and not (-1 or 0 or 1 or 2)) throw Rule(400, "IssueState 僅允許 -1、0、1、2。");
    }

    private static string Actor(string? value)
    {
        var actor = (value ?? "").Trim();
        if (actor.Length is 0 or > 10 || actor.Any(char.IsControl)) throw Rule(401, "登入者編號無效，無法寫入稽核欄位。");
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
        if (!TableExists(connection, transaction, name)) throw Rule(503, $"YMES 尚未建立 {name.ToUpperInvariant()}，請先比對來源 DDL 並套用 QC213 migration。");
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
    private static string? NullableText(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
    private static DateTime? NullableDate(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    private static Qc213RuleException Rule(int statusCode, string message) => new(statusCode, message);
    private static Qc213RuleException Conflict() => Rule(409, "資料已被其他作業修改，請重新查詢後再試。");

    private sealed record Qc213GroupValue(string GroupNo, string GroupName, int LotIsValid, string? Description);
}
