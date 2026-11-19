using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Operations;

public sealed class Op105Repository(NpgsqlDataSource dataSource)
{
    private const decimal MaxStamp = 99_999_999_999m;
    private const string GroupColumns = "opgroupno,opgroupname,psno,opgrouporder::int,description,erpno,creator,createdate,reviser,revisedate,datastamp";
    private const string MemberColumns = "m.opgroupno,m.opno,NULL::varchar,m.oproleflag::int,m.weightrate,m.creator,m.createdate,m.reviser,m.revisedate,m.datastamp";

    public IReadOnlyList<Op105GroupDto> List(string? opGroupNo, string? psNo)
    {
        var group = Search(opGroupNo); var segment = Search(psNo);
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand($"SELECT {GroupColumns} FROM \"YMES\".tblopgroupbasis WHERE (@group IS NULL OR position(lower(@group) in lower(opgroupno))>0) AND (@segment IS NULL OR position(lower(@segment) in lower(psno))>0) ORDER BY opgrouporder NULLS LAST,lower(btrim(opgroupno)) COLLATE \"C\",opgroupno COLLATE \"C\";", connection);
        Text(command, "group", group); Text(command, "segment", segment);
        using var reader = command.ExecuteReader(); var result = new List<Op105GroupDto>();
        while (reader.Read()) result.Add(MapGroup(reader));
        return result;
    }

    public Op105GroupDto Get(string opGroupNo)
    {
        using var connection = dataSource.OpenConnection();
        return ReadGroup(connection, null, Key(opGroupNo, "OPGroupNo", 20), false);
    }

    public Op105MembersDto Members(string opGroupNo)
    {
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var group = ReadGroup(connection, transaction, Key(opGroupNo, "OPGroupNo", 20), false, share: true);
        AssertChildOwners(connection, transaction, group.OpGroupNo);
        var rows = ReadMembers(connection, transaction, group.OpGroupNo);
        transaction.Commit(); return new(group, rows);
    }

    public Op105GroupDto Create(Op105GroupWriteRequest request, string actor)
    {
        actor = Actor(actor); var no = Key(request.OpGroupNo, "OPGroupNo", 20).ToUpperInvariant();
        var name = Name(request.OpGroupName); var description = Description(request.Description);
        ValidateOrder(request.OpGroupOrder, allowNull: false);
        var psNo = Key(request.PsNo, "PSNo", 50);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        Acquire(connection, transaction, no);
        using (var check = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblopgroupbasis WHERE lower(btrim(opgroupno))=lower(btrim(@no));", connection, transaction))
        { Text(check, "no", no); if (check.ExecuteScalar() is not null) throw Rule(409, "作業站群組編號已存在（不分大小寫／前後空白）。"); }
        using (var orphans = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblopgroupdetail WHERE lower(btrim(opgroupno))=lower(btrim(@no)) LIMIT 1;", connection, transaction))
        { Text(orphans, "no", no); if (orphans.ExecuteScalar() is not null) throw Rule(409, "此群組編號已有無主檔的舊明細，不能自動接管來源資料。"); }
        psNo = ApprovedSegment(connection, transaction, psNo);
        using var command = new NpgsqlCommand($"""
            INSERT INTO "YMES".tblopgroupbasis
                (opgroupno,opgroupname,psno,opgrouporder,description,erpno,datastamp,creator,createdate,reviser,revisedate)
            VALUES(@no,@name,@ps,@order,@description,NULL,0,@actor,now() AT TIME ZONE 'UTC',NULL,NULL)
            RETURNING {GroupColumns};
            """, connection, transaction);
        BindGroup(command, no, name, psNo, request.OpGroupOrder, description, actor);
        var result = ReturnedGroup(command); transaction.Commit(); return result;
    }

    public Op105GroupDto Update(string opGroupNo, Op105GroupUpdateRequest request, string actor)
    {
        actor = Actor(actor); ValidateStamp(request.DataStamp);
        var name = Name(request.OpGroupName); var description = Description(request.Description);
        ValidateOrder(request.OpGroupOrder, allowNull: true);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var current = LockGroup(connection, transaction, Key(opGroupNo, "OPGroupNo", 20));
        Expected(current.DataStamp, request.DataStamp); Incrementable(current.DataStamp);
        if (request.OpGroupNo is not null && !Same(connection, transaction, current.OpGroupNo, Key(request.OpGroupNo, "OPGroupNo", 20)))
            throw Rule(400, "修改時不可變更 OPGroupNo。");
        if (request.OpGroupOrder is null && current.OpGroupOrder is not null) throw Rule(400, "群組順序必須為 0–999999 整數。");
        var psNo = current.PsNo;
        if (!Same(connection, transaction, current.PsNo, request.PsNo))
        {
            psNo = ApprovedSegment(connection, transaction, Key(request.PsNo, "PSNo", 50));
            EnsureMemberScope(connection, transaction, current.OpGroupNo, psNo);
        }
        using var command = new NpgsqlCommand($"""
            UPDATE "YMES".tblopgroupbasis
            SET opgroupname=@name,psno=@ps,opgrouporder=@order,description=@description,
                reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE opgroupno=@no AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {GroupColumns};
            """, connection, transaction);
        BindGroup(command, current.OpGroupNo, name, psNo, request.OpGroupOrder, description, actor);
        Number(command, "stamp", request.DataStamp);
        var result = ReturnedGroup(command); transaction.Commit(); return result;
    }

    public void Delete(string opGroupNo, Op105ExpectedRequest request, string actor)
    {
        _ = Actor(actor); ValidateStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var current = LockGroup(connection, transaction, Key(opGroupNo, "OPGroupNo", 20));
        Expected(current.DataStamp, request.DataStamp);
        using (var children = new NpgsqlCommand("DELETE FROM \"YMES\".tblopgroupdetail WHERE opgroupno=@no;", connection, transaction))
        { Text(children, "no", current.OpGroupNo); children.ExecuteNonQuery(); }
        using (var command = new NpgsqlCommand("DELETE FROM \"YMES\".tblopgroupbasis WHERE opgroupno=@no AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction))
        {
            Text(command, "no", current.OpGroupNo); Number(command, "stamp", request.DataStamp);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
        }
        transaction.Commit();
    }

    public Op105MemberSavedDto AddMember(string opGroupNo, Op105MemberCreateRequest request, string actor)
    {
        actor = Actor(actor); ValidateMemberFields(request);
        var requestedOp = Key(request.OpNo, "OPNo", 20);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var group = LockGroup(connection, transaction, Key(opGroupNo, "OPGroupNo", 20)); Incrementable(group.DataStamp);
        var operation = ReadOperation(connection, transaction, requestedOp, required: true)!;
        if (operation.State != 2) throw Rule(422, "新增成員只能選已核准的作業站。");
        if (string.IsNullOrWhiteSpace(group.PsNo) || (group.PsNo.Trim(' ') != "*" && !Same(connection, transaction, group.PsNo, operation.PsNo)))
            throw Rule(422, "作業站不屬於群組生產區段；只有 PSNo=* 可使用所有區段。");
        if (ReadMember(connection, transaction, group.OpGroupNo, operation.No, required: false) is not null)
            throw Rule(409, "此作業站已加入目前群組。");
        using var command = new NpgsqlCommand($"""
            INSERT INTO "YMES".tblopgroupdetail AS m
                (opgroupno,opno,oproleflag,weightrate,datastamp,creator,createdate,reviser,revisedate)
            VALUES(@group,@op,@role,@weight,0,@actor,now() AT TIME ZONE 'UTC',NULL,NULL)
            RETURNING {MemberColumns};
            """, connection, transaction);
        BindMember(command, group.OpGroupNo, operation.No, request, actor);
        var result = ReturnedMember(command) with { OpName = operation.Name };
        var updated = BumpGroup(connection, transaction, group, actor);
        transaction.Commit(); return new(updated, result);
    }

    public Op105MemberSavedDto UpdateMember(string opGroupNo, string opNo, Op105MemberUpdateRequest request, string actor)
    {
        actor = Actor(actor); ValidateMemberFields(request); ValidateStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var group = LockGroup(connection, transaction, Key(opGroupNo, "OPGroupNo", 20)); Incrementable(group.DataStamp);
        var key = Key(opNo, "OPNo", 20);
        // Identity remains unchanged. A retired/missing legacy OP is not a new
        // assignment; role/weight edits retain it rather than inventing a master.
        var operation = ReadOperation(connection, transaction, key, required: false);
        var current = ReadMember(connection, transaction, group.OpGroupNo, key, required: true)!;
        Expected(current.DataStamp, request.DataStamp); Incrementable(current.DataStamp);
        using var command = new NpgsqlCommand($"""
            UPDATE "YMES".tblopgroupdetail AS m SET oproleflag=@role,weightrate=@weight,
                reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(m.datastamp,0)+1
            WHERE m.opgroupno=@group AND m.opno=@op AND m.datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {MemberColumns};
            """, connection, transaction);
        BindMember(command, group.OpGroupNo, current.OpNo, request, actor); Number(command, "stamp", request.DataStamp);
        var result = ReturnedMember(command) with { OpName = operation?.Name };
        var updated = BumpGroup(connection, transaction, group, actor);
        transaction.Commit(); return new(updated, result);
    }

    public Op105MemberDeletedDto DeleteMember(string opGroupNo, string opNo, Op105ExpectedRequest request, string actor)
    {
        actor = Actor(actor); ValidateStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var group = LockGroup(connection, transaction, Key(opGroupNo, "OPGroupNo", 20)); Incrementable(group.DataStamp);
        var key = Key(opNo, "OPNo", 20);
        _ = ReadOperation(connection, transaction, key, required: false);
        var current = ReadMember(connection, transaction, group.OpGroupNo, key, required: true)!;
        Expected(current.DataStamp, request.DataStamp);
        using var command = new NpgsqlCommand("DELETE FROM \"YMES\".tblopgroupdetail WHERE opgroupno=@group AND opno=@op AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction);
        Text(command, "group", group.OpGroupNo); Text(command, "op", current.OpNo); Number(command, "stamp", request.DataStamp);
        if (command.ExecuteNonQuery() != 1) throw Conflict();
        var updated = BumpGroup(connection, transaction, group, actor);
        transaction.Commit(); return new(updated);
    }

    public IReadOnlyList<Op105SegmentOption> Segments()
    {
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("SELECT psno,psname,issuestate::int FROM \"YMES\".tblprsproductionsegment WHERE issuestate=2 ORDER BY lower(btrim(psno)) COLLATE \"C\",psno COLLATE \"C\";", connection);
        using var reader = command.ExecuteReader();
        var result = new List<Op105SegmentOption> { new("*", "所有生產區段", null, true) };
        while (reader.Read()) if (reader.GetString(0).Trim(' ') != "*") result.Add(new(reader.GetString(0), String(reader, 1), Int(reader, 2), false));
        return result;
    }

    public IReadOnlyList<Op105OperationOption> Operations(string? psNo, int issueState)
    {
        if (issueState != 2) throw Rule(400, "作業站查找只提供 IssueState=2。");
        var ps = string.IsNullOrWhiteSpace(psNo) ? "*" : Key(psNo, "PSNo", 50);
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("SELECT opno,opname,psno,issuestate::int FROM \"YMES\".tblopbasis WHERE issuestate=2 AND (@ps='*' OR lower(btrim(psno))=lower(btrim(@ps))) ORDER BY lower(btrim(opno)) COLLATE \"C\",opno COLLATE \"C\";", connection);
        Text(command, "ps", ps); using var reader = command.ExecuteReader(); var result = new List<Op105OperationOption>();
        while (reader.Read()) result.Add(new(reader.GetString(0), reader.GetString(1), String(reader, 2), reader.GetInt32(3)));
        return result;
    }

    private static void Acquire(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        // YMES logical-key mutex. Group/OP identities are separate namespaces;
        // do not borrow OP103's shared Property collision restriction.
        using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:OP105:Group:' || lower(btrim(@no)),0));", connection, transaction);
        Text(command, "no", no); command.ExecuteNonQuery();
    }

    private static Op105GroupDto LockGroup(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        Acquire(connection, transaction, no);
        var result = ReadGroup(connection, transaction, no, true);
        AssertChildOwners(connection, transaction, result.OpGroupNo);
        return result;
    }

    private static Op105GroupDto ReadGroup(NpgsqlConnection connection, NpgsqlTransaction? transaction, string no, bool update, bool share = false)
    {
        using var command = new NpgsqlCommand($"SELECT {GroupColumns} FROM \"YMES\".tblopgroupbasis WHERE lower(btrim(opgroupno))=lower(btrim(@no)) ORDER BY lower(btrim(opgroupno)) COLLATE \"C\",opgroupno COLLATE \"C\"" + (update ? " FOR UPDATE;" : share ? " FOR SHARE;" : ";"), connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到指定的作業站群組。");
        var result = MapGroup(reader);
        if (reader.Read()) throw Rule(409, "OPGroupNo 存在大小寫／前後空白歧義，資料已保留。");
        return result;
    }

    private static void AssertChildOwners(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblopgroupdetail WHERE lower(btrim(opgroupno))=lower(btrim(@no)) AND opgroupno<>@no LIMIT 1;", connection, transaction);
        Text(command, "no", no);
        if (command.ExecuteScalar() is not null) throw Rule(409, "既有群組明細含 OPGroupNo 大小寫／空白別名，無法安全判定整組資料。");
    }

    private static IReadOnlyList<Op105MemberDto> ReadMembers(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("""
            SELECT m.opgroupno,m.opno,o.opname,m.oproleflag::int,m.weightrate,m.creator,m.createdate,m.reviser,m.revisedate,m.datastamp,
                   lower(btrim(m.opno)),count(o.opno) OVER(PARTITION BY m.opgroupno,m.opno)
            FROM "YMES".tblopgroupdetail m LEFT JOIN "YMES".tblopbasis o ON lower(btrim(o.opno))=lower(btrim(m.opno))
            WHERE m.opgroupno=@no ORDER BY lower(btrim(m.opno)) COLLATE "C",m.opno COLLATE "C";
            """, connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        var result = new List<Op105MemberDto>(); var names = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
        {
            if (reader.GetInt64(11) > 1 || !names.Add(reader.GetString(10))) throw Rule(409, "成員 OPNo 或作業站主檔存在大小寫／空白歧義，不能忽略或覆寫。");
            result.Add(MapMember(reader));
        }
        return result;
    }

    private static Op105MemberDto? ReadMember(NpgsqlConnection connection, NpgsqlTransaction transaction, string group, string op, bool required)
    {
        using var command = new NpgsqlCommand($"SELECT {MemberColumns} FROM \"YMES\".tblopgroupdetail m WHERE m.opgroupno=@group AND lower(btrim(m.opno))=lower(btrim(@op)) ORDER BY m.opno COLLATE \"C\" FOR UPDATE;", connection, transaction);
        Text(command, "group", group); Text(command, "op", op); using var reader = command.ExecuteReader();
        if (!reader.Read()) return required ? throw Rule(404, "找不到指定的群組作業站成員。") : null;
        var result = MapMember(reader);
        if (reader.Read()) throw Rule(409, "同一群組的成員 OPNo 存在大小寫／空白歧義，資料已保留。");
        return result;
    }

    private sealed record Operation(string No, string Name, string? PsNo, int? State);
    private static Operation? ReadOperation(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, bool required)
    {
        using var command = new NpgsqlCommand("SELECT opno,opname,psno,issuestate::int FROM \"YMES\".tblopbasis WHERE lower(btrim(opno))=lower(btrim(@no)) ORDER BY opno COLLATE \"C\" FOR SHARE;", connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) return required ? throw Rule(422, "作業站不存在，無法新增群組成員。") : null;
        var result = new Operation(reader.GetString(0), reader.GetString(1), String(reader, 2), Int(reader, 3));
        if (reader.Read()) throw Rule(409, "作業站主檔存在大小寫／空白歧義，無法安全選擇。");
        return result;
    }

    private static string ApprovedSegment(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        if (no == "*") return no;
        using var command = new NpgsqlCommand("SELECT psno,issuestate::int FROM \"YMES\".tblprsproductionsegment WHERE lower(btrim(psno))=lower(btrim(@no)) ORDER BY psno COLLATE \"C\" FOR SHARE;", connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(422, "生產區段不存在。");
        var result = reader.GetString(0); var state = Int(reader, 1);
        if (reader.Read()) throw Rule(409, "生產區段存在大小寫／空白歧義。");
        // YMES conservative policy, pending exact LoadPSDefine state filtering.
        if (state != 2) throw Rule(422, "新建或變更群組區段只能選擇已核准的生產區段。");
        return result;
    }

    private static void EnsureMemberScope(NpgsqlConnection connection, NpgsqlTransaction transaction, string group, string ps)
    {
        if (ps == "*") return;
        var members = ReadMembers(connection, transaction, group);
        var names = new List<string>();
        using (var order = new NpgsqlCommand("SELECT DISTINCT lower(btrim(value)) COLLATE \"C\" AS no FROM unnest(@names) AS input(value) ORDER BY no;", connection, transaction))
        {
            order.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = members.Select(m => m.OpNo).ToArray();
            using var reader = order.ExecuteReader(); while (reader.Read()) names.Add(reader.GetString(0));
        }
        foreach (var no in names)
        {
            var operation = ReadOperation(connection, transaction, no, required: false);
            if (operation is null || !Same(connection, transaction, operation.PsNo, ps))
                throw Rule(409, "既有成員不屬於新的生產區段，無法變更；請先調整成員或選擇所有區段 (*)。");
        }
    }

    private static Op105GroupDto BumpGroup(NpgsqlConnection connection, NpgsqlTransaction transaction, Op105GroupDto group, string actor)
    {
        // YMES extension: every cooperating member write advances the parent
        // version, so a stale header delete cannot erase newly changed members.
        using var command = new NpgsqlCommand($"UPDATE \"YMES\".tblopgroupbasis SET datastamp=COALESCE(datastamp,0)+1,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC' WHERE opgroupno=@no AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {GroupColumns};", connection, transaction);
        Text(command, "no", group.OpGroupNo); Text(command, "actor", actor); Number(command, "stamp", group.DataStamp);
        return ReturnedGroup(command);
    }

    private static void ValidateMemberFields(Op105MemberFieldsRequest request)
    {
        if (request.OpRoleFlag is not (0 or 1 or 2)) throw Rule(400, "OPRoleFlag 必須為 0、1 或 2。");
        if (request.WeightRate is not { } weight || weight is < 0 or > 100 || decimal.Round(weight, 2) != weight)
            throw Rule(400, "WeightRate 必須為 0–100，最多兩位小數。");
        // Entry/Exit counts and total weight are the source's Check operation,
        // not per-row constraints. Intermediate rows remain editable.
    }

    private static bool Same(NpgsqlConnection connection, NpgsqlTransaction transaction, string? left, string? right)
    {
        using var command = new NpgsqlCommand("SELECT lower(btrim(@left)) IS NOT DISTINCT FROM lower(btrim(@right));", connection, transaction);
        Text(command, "left", left); Text(command, "right", right); return command.ExecuteScalar() is true;
    }
    private static void ValidateOrder(int? value, bool allowNull)
    { if ((!allowNull && value is null) || value is < 0 or > 999999) throw Rule(400, "OPGroupOrder 必須為 0–999999 整數。"); }
    private static void ValidateStamp(decimal? value)
    { if (value is { } stamp && (stamp != decimal.Truncate(stamp) || stamp < -MaxStamp || stamp > MaxStamp)) throw Rule(400, "DataStamp 必須明確為 NULL 或 numeric(11,0) 範圍內整數。"); }
    private static void Expected(decimal? actual, decimal? expected) { if (actual != expected) throw Conflict(); }
    private static void Incrementable(decimal? stamp) { ValidateStamp(stamp); if (stamp == MaxStamp) throw Rule(409, "DataStamp 已達來源欄位上限，資料未變更。"); }
    private static string Key(string? value, string field, int length)
    { var text = value?.Trim(' ') ?? ""; if (text.Length is < 1 || text.Length > length || text.Any(char.IsControl)) throw Rule(400, $"{field} 必填、最多 {length} 字元，不可含控制字元。"); return text; }
    private static string? Search(string? value)
    { var text = value?.Trim(' '); if (text?.Length > 50 || text?.Any(char.IsControl) == true) throw Rule(400, "查詢文字不可超過 50 字元或含控制字元。"); return string.IsNullOrEmpty(text) ? null : text; }
    private static string Name(string? value)
    { if (string.IsNullOrWhiteSpace(value) || value.Length > 50 || value.Any(char.IsControl)) throw Rule(400, "群組名稱必填、最多 50 字元，不可含控制字元。"); return value; }
    private static string? Description(string? value)
    { if (value?.Length > 255 || value?.Any(c => char.IsControl(c) && c is not '\r' and not '\n' and not '\t') == true) throw Rule(400, "說明最多 255 字元，不可含無效控制字元。"); return string.IsNullOrEmpty(value) ? null : value; }
    private static string Actor(string value) => Key(value, "登入帳號", 10);
    private static string? String(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
    private static int? Int(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetInt32(index);
    private static DateTime? Date(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : DateTime.SpecifyKind(reader.GetDateTime(index), DateTimeKind.Utc);
    private static Op105GroupDto MapGroup(NpgsqlDataReader r) => new(r.GetString(0), r.GetString(1), String(r, 2), Int(r, 3), String(r, 4), String(r, 5), String(r, 6), Date(r, 7), String(r, 8), Date(r, 9), r.IsDBNull(10) ? null : r.GetDecimal(10));
    private static Op105MemberDto MapMember(NpgsqlDataReader r) => new(r.GetString(0), r.GetString(1), String(r, 2), r.GetInt32(3), r.GetDecimal(4), String(r, 5), Date(r, 6), String(r, 7), Date(r, 8), r.IsDBNull(9) ? null : r.GetDecimal(9));
    private static Op105GroupDto ReturnedGroup(NpgsqlCommand command)
    { using var reader = command.ExecuteReader(); if (!reader.Read()) throw Conflict(); var result = MapGroup(reader); if (reader.Read()) throw Conflict(); return result; }
    private static Op105MemberDto ReturnedMember(NpgsqlCommand command)
    { using var reader = command.ExecuteReader(); if (!reader.Read()) throw Conflict(); var result = MapMember(reader); if (reader.Read()) throw Conflict(); return result; }
    private static void BindGroup(NpgsqlCommand c, string no, string name, string? ps, int? order, string? description, string actor)
    { Text(c, "no", no); Text(c, "name", name); Text(c, "ps", ps); Number(c, "order", order); Text(c, "description", description); Text(c, "actor", actor); }
    private static void BindMember(NpgsqlCommand c, string group, string op, Op105MemberFieldsRequest request, string actor)
    { Text(c, "group", group); Text(c, "op", op); Number(c, "role", request.OpRoleFlag); Number(c, "weight", request.WeightRate); Text(c, "actor", actor); }
    private static void Text(NpgsqlCommand c, string name, string? value) => c.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand c, string name, decimal? value) => c.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static Op105RuleException Rule(int status, string message) => new(status, message);
    private static Op105RuleException Conflict() => Rule(409, "資料已被其他操作更新，請重新查詢；本次未保存。");
}
