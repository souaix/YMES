using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Processes;

public sealed class Ps102Repository(NpgsqlDataSource dataSource)
{
    private const string Columns = "stageno,stagename,stagetype,sequence,issuestate,creator,createdate,datastamp,reviser,revisedate";

    // The legacy list returns all states. The Vue radios filter this same result
    // without another request; nullable source states and stamps are preserved.
    public IReadOnlyList<Ps102StageDto> List(string? stageNo)
    {
        var query = SearchText(stageNo);
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null);
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tbl_fabprsstagebasis WHERE @query='' OR position(lower(@query) in lower(stageno))>0 ORDER BY stageno;", connection);
        Text(command, "query", query, 50);
        var result = new List<Ps102StageDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(Read(reader));
        return result;
    }

    public Ps102StageDto Create(Ps102StageWriteRequest request, string actor)
    {
        ValidateActor(actor);
        var value = Normalize(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            EnsureAvailable(connection, transaction, value, null);
            using var command = new NpgsqlCommand($"""
                INSERT INTO "YMES".tbl_fabprsstagebasis
                    (stageno,stagename,stagetype,sequence,issuestate,creator,createdate,datastamp)
                VALUES (@no,@name,@type,@sequence,0,@actor,now(),0)
                RETURNING {Columns};
                """, connection, transaction);
            Bind(command, value);
            Text(command, "actor", actor.Trim(), 10);
            var created = ReadReturned(command);
            transaction.Commit();
            return created;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Rollback(transaction);
            throw Rule(409, "StageNo 或 Sequence 已存在；Sequence 必須在全部狀態的製造層別中唯一。");
        }
        catch { Rollback(transaction); throw; }
    }

    public Ps102StageDto Update(string stageNo, Ps102StageUpdateRequest request, string actor)
    {
        ValidateActor(actor);
        var no = Key(stageNo);
        var value = Normalize(request);
        if (!string.Equals(no, value.No, StringComparison.OrdinalIgnoreCase)) throw Rule(400, "StageNo 不可修改。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = ReadForUpdate(connection, transaction, no);
            EnsureExpected(current, request.DataStamp);
            if (current.IssueState != 0) throw Rule(409, "只有未核准（IssueState=0）的製造層別可修改。");
            EnsureAvailable(connection, transaction, value, current.StageNo);
            using var command = new NpgsqlCommand($"""
                UPDATE "YMES".tbl_fabprsstagebasis
                SET stagename=@name,stagetype=@type,sequence=@sequence,
                    reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE stageno=@no AND issuestate=0 AND datastamp IS NOT DISTINCT FROM @stamp
                RETURNING {Columns};
                """, connection, transaction);
            Bind(command, value with { No = current.StageNo });
            Text(command, "actor", actor.Trim(), 10);
            Number(command, "stamp", request.DataStamp);
            var updated = ReadReturned(command);
            transaction.Commit();
            return updated;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Rollback(transaction);
            throw Rule(409, "Sequence 已經使用，請指定其他序號。");
        }
        catch { Rollback(transaction); throw; }
    }

    public Ps102StageDto Delete(string stageNo, Ps102StageStateRequest request, string actor)
    {
        ValidateActor(actor);
        var no = Key(stageNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = ReadForUpdate(connection, transaction, no);
            EnsureExpected(current, request.DataStamp);
            if (current.IssueState == 1) throw Rule(409, "簽核中的製造層別不可刪除。");
            Ps102StageDto result;
            if (current.IssueState == 2)
            {
                result = ChangeState(connection, transaction, current, -1, actor);
            }
            else if (current.IssueState is 0 or -1)
            {
                // The source deletes only the stage row. There is no verified
                // cascade into PS104 NodeBasis, so dependent rows are not touched.
                using var command = new NpgsqlCommand("""
                    DELETE FROM "YMES".tbl_fabprsstagebasis
                    WHERE stageno=@no AND issuestate IS NOT DISTINCT FROM @state
                        AND datastamp IS NOT DISTINCT FROM @stamp;
                    """, connection, transaction);
                BindExpected(command, current);
                if (command.ExecuteNonQuery() != 1) throw Conflict();
                result = current;
            }
            else throw Rule(409, "目前資料狀態不允許刪除，請確認製造層別狀態。");
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Ps102StageDto Approve(string stageNo, Ps102StageStateRequest request, string actor)
    {
        ValidateActor(actor);
        var no = Key(stageNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = ReadForUpdate(connection, transaction, no);
            EnsureExpected(current, request.DataStamp);
            if (current.IssueState != 0) throw Rule(409, "只有未核准（IssueState=0）的製造層別可送出核准。");
            // Until the shared workflow is integrated, the absence of all known
            // Issue/Job tables is the only state in which direct approval is safe.
            foreach (var table in new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" })
            {
                if (TableExists(connection, transaction, table))
                    throw Rule(503, "YMES 已存在 Issue/Job 表；PS102 尚未接入共用簽核服務，資料未變更。");
            }
            var result = ChangeState(connection, transaction, current, 2, actor);
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    private static Ps102StageDto ChangeState(NpgsqlConnection connection, NpgsqlTransaction transaction,
        Ps102StageDto current, int next, string actor)
    {
        using var command = new NpgsqlCommand($"""
            UPDATE "YMES".tbl_fabprsstagebasis
            SET issuestate=@next,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
            WHERE stageno=@no AND issuestate IS NOT DISTINCT FROM @state
                AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {Columns};
            """, connection, transaction);
        BindExpected(command, current);
        Number(command, "next", next);
        Text(command, "actor", actor.Trim(), 10);
        return ReadReturned(command);
    }

    private static Ps102StageDto ReadForUpdate(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tbl_fabprsstagebasis WHERE lower(stageno)=lower(@no) FOR UPDATE;", connection, transaction);
        Text(command, "no", no, 50);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, $"找不到製造層別 {no}。");
        var result = Read(reader);
        if (reader.Read()) throw Rule(409, "StageNo 存在大小寫重複資料，請先修正資料。");
        return result;
    }

    private static Ps102StageDto ReadReturned(NpgsqlCommand command)
    {
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Conflict();
        return Read(reader);
    }

    private static Ps102StageDto Read(NpgsqlDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), reader.GetString(2), Convert.ToInt32(reader.GetValue(3)),
        reader.IsDBNull(4) ? null : Convert.ToInt32(reader.GetValue(4)),
        reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetDateTime(6),
        reader.IsDBNull(7) ? null : reader.GetDecimal(7), reader.IsDBNull(8) ? null : reader.GetString(8),
        reader.IsDBNull(9) ? null : reader.GetDateTime(9));

    private sealed record StageValue(string No, string Name, string Type, decimal Sequence);
    private static StageValue Normalize(Ps102StageWriteRequest request)
    {
        var no = Key(request.StageNo);
        var name = RequiredText(request.StageName, "StageName", 50);
        var type = RequiredText(request.StageType, "StageType", 50);
        if (request.Sequence != decimal.Truncate(request.Sequence) || request.Sequence < 1 || request.Sequence > 9999)
            throw Rule(400, "Sequence 必須是 1 到 9999 的整數。");
        return new(no, name, type, request.Sequence);
    }

    private static void EnsureAvailable(NpgsqlConnection connection, NpgsqlTransaction transaction, StageValue value, string? exceptNo)
    {
        using var command = new NpgsqlCommand("""
            SELECT stageno,sequence FROM "YMES".tbl_fabprsstagebasis
            WHERE (@except IS NULL OR stageno<>@except)
                AND (lower(stageno)=lower(@no) OR sequence=@sequence);
            """, connection, transaction);
        Text(command, "no", value.No, 50);
        Text(command, "except", exceptNo, 50);
        Number(command, "sequence", value.Sequence);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return;
        if (string.Equals(reader.GetString(0), value.No, StringComparison.OrdinalIgnoreCase))
            throw Rule(409, "StageNo 已存在。");
        throw Rule(409, $"Sequence {value.Sequence} 已經使用；序號必須在全部狀態的製造層別中唯一。");
    }

    private static void EnsureExpected(Ps102StageDto current, decimal? expectedStamp)
    {
        if (current.DataStamp != expectedStamp) throw Conflict();
    }

    private static void Bind(NpgsqlCommand command, StageValue value)
    {
        Text(command, "no", value.No, 50);
        Text(command, "name", value.Name, 50);
        Text(command, "type", value.Type, 50);
        Number(command, "sequence", value.Sequence);
    }

    private static void BindExpected(NpgsqlCommand command, Ps102StageDto current)
    {
        Text(command, "no", current.StageNo, 50);
        Number(command, "state", current.IssueState);
        Number(command, "stamp", current.DataStamp);
    }

    private static string Key(string? value) => RequiredText(value, "StageNo", 50).ToUpperInvariant();
    private static string RequiredText(string? value, string field, int max)
    {
        var text = (value ?? "").Trim();
        if (text.Length == 0 || text.Length > max || text.Any(char.IsControl))
            throw Rule(400, $"{field} 為必填，最長 {max} 個字元且不可含控制字元。");
        return text;
    }

    private static string SearchText(string? value)
    {
        var text = (value ?? "").Trim();
        if (text.Length > 50 || text.Any(char.IsControl)) throw Rule(400, "StageNo 查詢條件最長 50 個字元且不可含控制字元。");
        return text;
    }

    private static void ValidateActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor) || actor.Trim().Length > 10 || actor.Any(char.IsControl))
            throw Rule(401, "登入帳號必須有效，且不可超過 10 個字元。");
    }

    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@table AND c.relkind IN ('r','p','v','m','f'));", connection, transaction);
        Text(command, "table", table, 63);
        return command.ExecuteScalar() is true;
    }

    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction)
    {
        if (!TableExists(connection, transaction, "tbl_fabprsstagebasis"))
            throw Rule(503, "YMES 尚未建立製造層別資料表 TBL_FABPRSSTAGEBASIS，請先完成 PS102 migration。");
    }

    private static void Text(NpgsqlCommand command, string name, string? value, int size) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand command, string name, decimal? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static void Rollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { /* retain original failure */ } }
    private static Ps102RuleException Rule(int status, string message) => new(status, message);
    private static Ps102RuleException Conflict() => Rule(409, "資料已被其他作業修改，請重新查詢後再試。");
}
