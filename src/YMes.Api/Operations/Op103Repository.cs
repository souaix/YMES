using System.Data;
using MES.Api.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Operations;

public sealed class Op103Repository(NpgsqlDataSource dataSource)
{
    private const string Columns = "optype,description,creator,createdate,issuestate::int,datastamp,reviser,revisedate";
    private const decimal MaxStamp = 99999999999m;

    public IReadOnlyList<Op103TypeDto> List(int? state, string? search)
    {
        if (state.HasValue && state is not (-1 or 0 or 1 or 2)) throw Rule(400, "查詢狀態只允許0、1、2、-1。");
        var term = Description(search, 20, false);
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tbloptype WHERE (@state IS NULL OR issuestate=@state) AND (@search IS NULL OR strpos(lower(optype),lower(@search))>0) ORDER BY optype;", connection);
        Number(command, "state", state); Text(command, "search", term, 20);
        using var reader = command.ExecuteReader();
        var result = new List<Op103TypeDto>();
        while (reader.Read()) result.Add(Read(reader));
        return result;
    }

    public Op103TypeDto Get(string opType)
    {
        using var connection = dataSource.OpenConnection();
        return Find(connection, null, Key(opType), false);
    }

    public Op103TypeDto Create(Op103WriteRequest request, string actor)
    {
        var key = Key(request.OpType).ToUpperInvariant();
        var description = Description(request.Description); actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Op103IdentifierGuard.EnsureNewType(connection, transaction, key);
        using var command = new NpgsqlCommand($"""
            INSERT INTO "YMES".tbloptype(optype,description,creator,createdate,issuestate)
            VALUES(@type,@description,@actor,now() AT TIME ZONE 'UTC',0) RETURNING {Columns};
            """, connection, transaction);
        Text(command, "type", key, 20); Text(command, "description", description, 255); Text(command, "actor", actor, 10);
        var result = Returned(command); transaction.Commit(); return result;
    }

    public Op103TypeDto Update(string opType, Op103UpdateRequest request, string actor)
    {
        var key = Key(opType);
        if (!string.Equals(key, Key(request.OpType), StringComparison.OrdinalIgnoreCase)) throw Rule(400, "修改時不可變更作業站類別編號。");
        var description = Description(request.Description); actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Op103IdentifierGuard.LockIdentifier(connection, transaction, key);
        var current = Find(connection, transaction, key, true);
        Expected(current, request.IssueState, request.DataStamp);
        if (current.IssueState != 0) throw Rule(409, "只有未簽核的作業站類別可以修改。");
        Advance(current.DataStamp);
        using var command = new NpgsqlCommand($"""
            UPDATE "YMES".tbloptype SET description=@description,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE optype=@type AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {Columns};
            """, connection, transaction);
        Bind(command, current, actor); Text(command, "description", description, 255);
        var result = Returned(command); transaction.Commit(); return result;
    }

    public Op103TypeDto Approve(string opType, Op103CommandRequest request, string actor)
    {
        var key = Key(opType); actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Op103IdentifierGuard.LockIdentifier(connection, transaction, key);
        var current = Find(connection, transaction, key, true);
        Expected(current, request.IssueState, request.DataStamp);
        if (current.IssueState != 0) throw Rule(409, "只有未簽核的作業站類別可以核准。");
        if (new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" }.Any(table => Op103IdentifierGuard.Exists(connection, transaction, table)))
            throw Rule(503, "作業站類別尚未接入現存 OP TYPE Issue/Job 簽核服務；資料未變更。");
        var result = ChangeState(connection, transaction, current, 2, actor);
        transaction.Commit(); return result;
    }

    public Op103DeleteResult Delete(string opType, Op103CommandRequest request, string actor)
    {
        var key = Key(opType); actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Op103IdentifierGuard.LockIdentifier(connection, transaction, key);
        var current = Find(connection, transaction, key, true);
        Expected(current, request.IssueState, request.DataStamp);
        if (current.IssueState == 2)
        {
            var retired = ChangeState(connection, transaction, current, -1, actor);
            transaction.Commit(); return new("UNUSED", retired);
        }
        if (current.IssueState is not (0 or -1)) throw Rule(409, "簽核中不可刪除，只有未簽核／不使用類別可以實體刪除。");
        // YMES integrity extension: retain referenced types; source separately
        // establishes the OPNo/Type property namespace. Never reverse-lock OP.
        using (var reference = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblopbasis WHERE lower(btrim(optype))=lower(btrim(@type)) OR lower(btrim(opno))=lower(btrim(@type)));", connection, transaction))
        {
            Text(reference, "type", current.OpType, 20);
            if (reference.ExecuteScalar() is true) throw Rule(409, "作業站仍引用此類別，或有同名作業站造成屬性歸屬不明，資料已保留。");
        }
        // Exact source-defined shared cleanup; missing/unreadable dependency is
        // a database failure, not an empty provider or an optional fake table.
        using (var properties = new NpgsqlCommand("DELETE FROM \"YMES\".tblopproperty WHERE lower(btrim(opno))=lower(btrim(@type));", connection, transaction))
        { Text(properties, "type", current.OpType, 20); properties.ExecuteNonQuery(); }
        using (var command = new NpgsqlCommand($"DELETE FROM \"YMES\".tbloptype WHERE optype=@type AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {Columns};", connection, transaction))
        { Bind(command, current, actor); current = Returned(command); }
        transaction.Commit(); return new("DELETED", current);
    }

    private static Op103TypeDto ChangeState(NpgsqlConnection connection, NpgsqlTransaction transaction, Op103TypeDto current, int state, string actor)
    {
        Advance(current.DataStamp);
        using var command = new NpgsqlCommand($"""
            UPDATE "YMES".tbloptype SET issuestate=@next,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE optype=@type AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {Columns};
            """, connection, transaction);
        Bind(command, current, actor); Number(command, "next", state); return Returned(command);
    }
    private static Op103TypeDto Find(NpgsqlConnection connection, NpgsqlTransaction? transaction, string key, bool update)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tbloptype WHERE lower(btrim(optype))=lower(@type) ORDER BY optype" + (update ? " FOR UPDATE;" : ";"), connection, transaction);
        Text(command, "type", key, 20); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到指定作業站類別。");
        var result = Read(reader);
        if (reader.Read()) throw Rule(409, "作業站類別存在大小寫／空白歧義，請先整理主檔。");
        return result;
    }
    private static Op103TypeDto Returned(NpgsqlCommand command)
    { using var reader = command.ExecuteReader(); return reader.Read() ? Read(reader) : throw Rule(409, "資料已變更，請重新查詢。"); }
    private static Op103TypeDto Read(NpgsqlDataReader reader) => new(reader.GetString(0), String(reader, 1), String(reader, 2), Date(reader, 3), reader.GetInt32(4), reader.IsDBNull(5) ? null : reader.GetDecimal(5), String(reader, 6), Date(reader, 7));
    private static string? String(NpgsqlDataReader reader, int i) => reader.IsDBNull(i) ? null : reader.GetString(i);
    private static DateTime? Date(NpgsqlDataReader reader, int i) => reader.IsDBNull(i) ? null : DateTime.SpecifyKind(reader.GetDateTime(i), DateTimeKind.Utc);
    private static void Expected(Op103TypeDto current, int? state, decimal? stamp)
    {
        if (state is not (-1 or 0 or 1 or 2)) throw Rule(400, "必須明確提供有效IssueState。");
        if (stamp.HasValue && (stamp < -MaxStamp || stamp > MaxStamp || decimal.Truncate(stamp.Value) != stamp)) throw Rule(400, "DataStamp必須為11位整數範圍或明確NULL。");
        if (current.IssueState != state || current.DataStamp != stamp) throw Rule(409, "資料已由其他操作變更，請重新查詢。");
    }
    private static void Advance(decimal? stamp) { if (stamp >= MaxStamp) throw Rule(409, "DataStamp已達上限，不能再遞增。"); }
    private static string Key(string? value) => Op103IdentifierGuard.Key(value);
    private static string? Description(string? value, int maximum = 255, bool multiline = true)
    {
        var text = value?.Trim();
        if (text?.Length > maximum || text?.Any(c => char.IsControl(c) && !(multiline && (c is '\r' or '\n' or '\t'))) == true) throw Rule(400, $"文字最多{maximum}字，不可含非文字控制字元。");
        return string.IsNullOrEmpty(text) ? null : text;
    }
    private static string Actor(string? actor)
    { var value = actor?.Trim() ?? ""; if (value.Length is < 1 or > 10 || value.Any(char.IsControl)) throw Rule(400, "登入帳號不符合稽核欄位。"); return value; }
    private static void Bind(NpgsqlCommand command, Op103TypeDto current, string actor)
    { Text(command, "type", current.OpType, 20); Number(command, "state", current.IssueState); Number(command, "stamp", current.DataStamp); Text(command, "actor", actor, 10); }
    private static void Text(NpgsqlCommand command, string name, string? value, int size) => Op103IdentifierGuard.Text(command, name, value, size);
    private static void Number(NpgsqlCommand command, string name, decimal? value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static MasterDataRuleException Rule(int status, string message) => Op103IdentifierGuard.Rule(status, message);
}
