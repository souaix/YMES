using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Processes;

public sealed class Ps103Repository(NpgsqlDataSource dataSource)
{
    private const string Columns = "processtype,description,issuestate::int,datastamp,creator,createdate,reviser,revisedate";

    public IReadOnlyList<Ps103TypeDto> List(int? issueState, string? processType)
    {
        if (issueState.HasValue && issueState.Value is not (-1 or 0 or 1 or 2)) throw Rule(400, "IssueState 只允許 -1、0、1、2。");
        var search = (processType ?? "").Trim();
        if (search.Length > 20 || search.Any(char.IsControl)) throw Rule(400, "Process Type 查詢最多 20 字元，不可含控制字元。");
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null);
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblprstype WHERE (@state IS NULL OR issuestate=@state) AND (@search='' OR position(lower(@search) in lower(processtype))>0) ORDER BY processtype;", connection);
        Number(command, "state", issueState); Text(command, "search", search, 20);
        var result = new List<Ps103TypeDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(Read(reader));
        return result;
    }

    public Ps103TypeDto Get(string processType)
    {
        var no = Key(processType);
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null);
        return ReadOne(connection, null, no, false);
    }

    public Ps103TypeDto Create(Ps103TypeWriteRequest request, string actor)
    {
        actor = Actor(actor);
        var no = Key(request.ProcessType);
        var description = Description(request.Description);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            PrsIdentifierGuard.EnsureTypeIdentifierAvailable(connection, transaction, no);
            EnsureTable(connection, transaction);
            using (var duplicate = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblprstype WHERE lower(processtype)=lower(@no);", connection, transaction))
            {
                Text(duplicate, "no", no, 20);
                if (duplicate.ExecuteScalar() is not null) throw Rule(409, "Process Type 已存在。");
            }
            using var command = new NpgsqlCommand($"""
                INSERT INTO "YMES".tblprstype(processtype,description,creator,createdate,issuestate)
                VALUES(@no,@description,@actor,now() AT TIME ZONE 'UTC',0) RETURNING {Columns};
                """, connection, transaction);
            Text(command, "no", no, 20); Text(command, "description", description, 255); Text(command, "actor", actor, 10);
            var result = Returned(command);
            transaction.Commit(); return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Rollback(transaction); throw Rule(409, "Process Type 已存在。");
        }
        catch { Rollback(transaction); throw; }
    }

    public Ps103TypeDto Update(string processType, Ps103TypeUpdateRequest request, string actor)
    {
        actor = Actor(actor);
        var no = Key(processType);
        if (!string.IsNullOrWhiteSpace(request.ProcessType) && !string.Equals(no, Key(request.ProcessType), StringComparison.Ordinal)) throw Rule(400, "Process Type 不可修改。");
        var description = Description(request.Description);
        Stamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            PrsIdentifierGuard.LockIdentifier(connection, transaction, no);
            EnsureTable(connection, transaction);
            var current = ReadOne(connection, transaction, no, true);
            Expected(current, request.DataStamp);
            if (current.IssueState != 0) throw Rule(409, "只有未核准（狀態 0）的流程類別可以修改。");
            EnsureStampCanAdvance(current.DataStamp);
            using var command = new NpgsqlCommand($"""
                UPDATE "YMES".tblprstype SET description=@description,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
                WHERE processtype=@no AND issuestate=0 AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {Columns};
                """, connection, transaction);
            BindExpected(command, current); Text(command, "description", description, 255); Text(command, "actor", actor, 10);
            var result = Returned(command);
            transaction.Commit(); return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Ps103TypeDto Delete(string processType, Ps103DeleteRequest request, string actor)
    {
        actor = Actor(actor);
        var no = Key(processType);
        Stamp(request.DataStamp);
        if (request.IssueState is not (-1 or 0 or 1 or 2)) throw Rule(400, "刪除必須提供有效 IssueState（-1、0、1、2）。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            PrsIdentifierGuard.LockIdentifier(connection, transaction, no);
            EnsureTable(connection, transaction);
            var current = ReadOne(connection, transaction, no, true);
            Expected(current, request.DataStamp);
            if (current.IssueState != request.IssueState) throw Conflict();
            if (current.IssueState == 1) throw Rule(409, "簽核中的流程類別不可刪除。");
            Ps103TypeDto result;
            if (current.IssueState == 2) result = ChangeState(connection, transaction, current, -1, actor);
            else if (current.IssueState is 0 or -1)
            {
                EnsureNoProcessReferences(connection, transaction, current.ProcessType);
                // Source deletes every version under this ProcessNo, including
                // -1 sentinel; it does not copy properties during Add/Copy.
                if (PrsIdentifierGuard.Exists(connection, transaction, "tblprsprocessproperty"))
                {
                    using var properties = new NpgsqlCommand("DELETE FROM \"YMES\".tblprsprocessproperty WHERE lower(processno)=lower(@no);", connection, transaction);
                    Text(properties, "no", current.ProcessType, 20); properties.ExecuteNonQuery();
                }
                using var command = new NpgsqlCommand($"DELETE FROM \"YMES\".tblprstype WHERE processtype=@no AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {Columns};", connection, transaction);
                BindExpected(command, current); result = Returned(command);
            }
            else throw Rule(409, "目前流程類別狀態不允許刪除。");
            transaction.Commit(); return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Ps103TypeDto Approve(string processType, Ps103StampRequest request, string actor)
    {
        actor = Actor(actor);
        var no = Key(processType);
        Stamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            PrsIdentifierGuard.LockIdentifier(connection, transaction, no);
            EnsureTable(connection, transaction);
            var current = ReadOne(connection, transaction, no, true);
            Expected(current, request.DataStamp);
            if (current.IssueState != 0) throw Rule(409, "只有未核准（狀態 0）的流程類別可核准。");
            if (new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" }.Any(table => PrsIdentifierGuard.Exists(connection, transaction, table)))
                throw Rule(503, "YMES 已存在 Issue/Job 表，PS103 尚未接入 PRS TYPE 共用簽核服務；資料未變更。");
            var result = ChangeState(connection, transaction, current, 2, actor);
            transaction.Commit(); return result;
        }
        catch { Rollback(transaction); throw; }
    }

    private static void EnsureNoProcessReferences(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        if (!PrsIdentifierGuard.Exists(connection, transaction, "tblprsprocessbasis")) return;
        using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblprsprocessbasis WHERE lower(processtype)=lower(@no) OR lower(processno)=lower(@no) LIMIT 1;", connection, transaction);
        Text(command, "no", no, 20);
        if (command.ExecuteScalar() is not null) throw Rule(409, "此類別已被流程引用，或有同名 ProcessNo 造成屬性歸屬不明，不可實體刪除。");
    }

    private static Ps103TypeDto ChangeState(NpgsqlConnection connection, NpgsqlTransaction transaction, Ps103TypeDto current, int next, string actor)
    {
        EnsureStampCanAdvance(current.DataStamp);
        using var command = new NpgsqlCommand($"""
            UPDATE "YMES".tblprstype SET issuestate=@next,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE processtype=@no AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {Columns};
            """, connection, transaction);
        BindExpected(command, current); Number(command, "next", next); Text(command, "actor", actor, 10);
        return Returned(command);
    }

    private static Ps103TypeDto ReadOne(NpgsqlConnection connection, NpgsqlTransaction? transaction, string no, bool forUpdate)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblprstype WHERE lower(processtype)=lower(@no) ORDER BY processtype{(forUpdate ? " FOR UPDATE" : "")};", connection, transaction);
        Text(command, "no", no, 20);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到指定流程類別。");
        var result = Read(reader);
        if (reader.Read()) throw Rule(409, "Process Type 存在大小寫不同的重複資料，請先整理。");
        return result;
    }
    private static Ps103TypeDto Returned(NpgsqlCommand command)
    {
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Conflict();
        return Read(reader);
    }
    private static Ps103TypeDto Read(NpgsqlDataReader r) => new(r.GetString(0), String(r, 1), r.IsDBNull(2) ? null : r.GetInt32(2),
        r.IsDBNull(3) ? null : r.GetDecimal(3), String(r, 4), Date(r, 5), String(r, 6), Date(r, 7));
    private static void BindExpected(NpgsqlCommand command, Ps103TypeDto current)
    {
        Text(command, "no", current.ProcessType, 20); Number(command, "state", current.IssueState); Number(command, "stamp", current.DataStamp);
    }
    private static void Expected(Ps103TypeDto current, decimal? stamp) { if (current.DataStamp != stamp) throw Conflict(); }
    private static void Stamp(decimal? stamp)
    {
        if (stamp.HasValue && (stamp.Value != decimal.Truncate(stamp.Value) || stamp.Value < -99999999999m || stamp.Value > 99999999999m)) throw Rule(400, "DataStamp 必須為 NULL 或 11 位數範圍內的整數。");
    }
    private static void EnsureStampCanAdvance(decimal? stamp)
    {
        if ((stamp ?? 0m) >= 99999999999m) throw Rule(409, "流程類別 DataStamp 已達資料表上限，無法再修改狀態或內容；資料未變更。");
    }
    private static string Key(string? input)
    {
        var value = (input ?? "").Trim().ToUpperInvariant();
        if (value.Length is < 1 or > 20 || value.Any(char.IsControl)) throw Rule(400, "Process Type 必填、最多 20 字元，不可含控制字元。");
        return value;
    }
    private static string? Description(string? input)
    {
        var value = (input ?? "").Trim();
        if (value.Length > 255 || value.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t'))) throw Rule(400, "Description 最多 255 字元，不可含無效控制字元。");
        return value.Length == 0 ? null : value;
    }
    private static string Actor(string actor)
    {
        var value = (actor ?? "").Trim();
        if (value.Length is < 1 or > 10 || value.Any(char.IsControl)) throw Rule(401, "登入帳號無效或超過 10 字元。");
        return value;
    }
    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction)
    {
        if (!PrsIdentifierGuard.Exists(connection, transaction, "tblprstype")) throw Rule(503, "YMES 尚未建立 TBLPRSTYPE，請先核對來源 DDL 及 migration。");
    }
    private static void Text(NpgsqlCommand command, string name, string? value, int size) => command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand command, string name, decimal? value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static string? String(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
    // YMES API audit writers store UTC wall values in source-compatible timestamp
    // columns. Mark only these audit values as UTC at the serialization boundary.
    private static DateTime? Date(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : DateTime.SpecifyKind(reader.GetDateTime(index), DateTimeKind.Utc);
    private static void Rollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { /* Preserve original error. */ } }
    private static Ps103RuleException Rule(int status, string message) => new(status, message);
    private static Ps103RuleException Conflict() => Rule(409, "流程類別版本或狀態已變更，請重新查詢後再操作。");
}
