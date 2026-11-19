using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc503Repository(NpgsqlDataSource dataSource)
{
    private const string Columns = "inspdriverno,functionname,description,creator,createdate,issuestate::int,datastamp,reviser,revisedate";
    public static IReadOnlyList<string> AllowedFunctions { get; } =
        Array.AsReadOnly(new[] { "funDrv_Thick", "funDrv_Channel", "funDrFunctionC" });

    public IReadOnlyList<Qc503DriverDto> List(int? issueState, string? search)
    {
        if (issueState.HasValue) ValidateState(issueState.Value);
        var query = (search ?? "").Trim();
        if (query.Length > 25 || query.Any(char.IsControl))
            throw Rule(400, "InspDriverNo 查詢條件不可超過 25 個字元或含控制字元。");
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null);
        using var command = new NpgsqlCommand($"""
            SELECT {Columns} FROM "YMES".tblqcinspdriver
            WHERE (@state IS NULL OR issuestate=@state)
              AND (@search='' OR position(lower(@search) in lower(inspdriverno))>0)
            ORDER BY inspdriverno;
            """, connection);
        AddNumeric(command, "state", issueState.HasValue ? (decimal)issueState.Value : null);
        AddText(command, "search", query, 25);
        var result = new List<Qc503DriverDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(Read(reader));
        return result;
    }

    public Qc503DriverDto Get(string key)
    {
        var no = NormalizeKey(key);
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null);
        return Read(connection, null, no, false);
    }

    public Qc503DriverDto Create(Qc503DriverWriteRequest request, string actor)
    {
        actor = NormalizeActor(actor);
        var value = Normalize(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            using (var duplicate = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblqcinspdriver WHERE lower(inspdriverno)=lower(@no);", connection, transaction))
            {
                AddText(duplicate, "no", value.No, 25);
                if (duplicate.ExecuteScalar() is not null) throw Rule(409, "InspDriverNo 已存在。");
            }
            using var command = new NpgsqlCommand("""
                INSERT INTO "YMES".tblqcinspdriver
                    (inspdriverno,functionname,description,creator,createdate,issuestate,datastamp)
                VALUES (@no,@function,@description,@actor,now(),0,0);
                """, connection, transaction);
            Bind(command, value);
            AddText(command, "actor", actor, 10);
            command.ExecuteNonQuery();
            var result = Read(connection, transaction, value.No, false);
            transaction.Commit();
            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Rollback(transaction);
            throw Rule(409, "InspDriverNo 已存在。");
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc503DriverDto Update(string key, Qc503DriverUpdateRequest request, string actor)
    {
        actor = NormalizeActor(actor);
        var no = NormalizeKey(key);
        var value = Normalize(request);
        if (!string.Equals(no, value.No, StringComparison.Ordinal)) throw Rule(400, "InspDriverNo 不可修改。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = Read(connection, transaction, no, true);
            EnsureExpected(current, request.IssueState, request.DataStamp);
            if (current.IssueState == 1) throw Rule(409, "簽核中的檢測 Driver 不可修改。");
            if (current.IssueState != 0) throw Rule(409, "僅未簽核的檢測 Driver 可修改；已簽核或不使用資料不可直接編輯。");
            using var command = new NpgsqlCommand("""
                UPDATE "YMES".tblqcinspdriver
                SET functionname=@function,description=@description,reviser=@actor,revisedate=now(),
                    datastamp=COALESCE(datastamp,0)+1
                WHERE lower(inspdriverno)=lower(@no) AND issuestate=@state
                  AND datastamp IS NOT DISTINCT FROM @stamp;
                """, connection, transaction);
            Bind(command, value);
            BindExpected(command, current, actor);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
            var result = Read(connection, transaction, no, false);
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc503DriverDto Delete(string key, int issueState, decimal? dataStamp, string actor)
    {
        actor = NormalizeActor(actor);
        var no = NormalizeKey(key);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            // QC501/QC502 acquire FOR KEY SHARE on this same Driver row before
            // linking it. FOR UPDATE waits for those writes, then the reference
            // checks below see their committed rows under READ COMMITTED.
            var current = Read(connection, transaction, no, true);
            EnsureExpected(current, issueState, dataStamp);
            if (current.IssueState == 1) throw Rule(409, "簽核中的檢測 Driver 不可刪除。");
            Qc503DriverDto result;
            if (current.IssueState == 2)
            {
                ChangeState(connection, transaction, current, actor, -1);
                result = Read(connection, transaction, no, false);
            }
            else
            {
                EnsureNoReferences(connection, transaction, no);
                using var command = new NpgsqlCommand("""
                    DELETE FROM "YMES".tblqcinspdriver
                    WHERE lower(inspdriverno)=lower(@no) AND issuestate=@state
                      AND datastamp IS NOT DISTINCT FROM @stamp;
                    """, connection, transaction);
                AddText(command, "no", no, 25);
                AddNumeric(command, "state", current.IssueState);
                AddNumeric(command, "stamp", current.DataStamp);
                if (command.ExecuteNonQuery() != 1) throw Conflict();
                result = current;
            }
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc503DriverDto Approve(string key, Qc503DriverStateRequest request, string actor)
    {
        actor = NormalizeActor(actor);
        if (request.IssueState != 0) throw Rule(400, "核准只接受未簽核狀態。");
        var no = NormalizeKey(key);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = Read(connection, transaction, no, true);
            EnsureExpected(current, request.IssueState, request.DataStamp);
            if (current.IssueState != 0) throw Rule(409, "僅未簽核的檢測 Driver 可核准。");
            if (new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" }
                .Any(table => TableExists(connection, transaction, table)))
                throw Rule(503, "YMES 已存在 Issue/Job 流程表，但 QC503 尚未接入共用簽核服務；資料未變更。");
            ChangeState(connection, transaction, current, actor, 2);
            var result = Read(connection, transaction, no, false);
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    private static Qc503DriverDto Read(NpgsqlConnection connection, NpgsqlTransaction? transaction, string no, bool forUpdate)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblqcinspdriver WHERE lower(inspdriverno)=lower(@no){(forUpdate ? " FOR UPDATE" : "")};", connection, transaction);
        AddText(command, "no", no, 25);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, $"找不到檢測 Driver {no}。");
        return Read(reader);
    }

    private static Qc503DriverDto Read(NpgsqlDataReader r) => new(
        r.GetString(0), NullableText(r, 1), NullableText(r, 2), NullableText(r, 3), NullableDate(r, 4), r.GetInt32(5),
        r.IsDBNull(6) ? null : r.GetDecimal(6), NullableText(r, 7), NullableDate(r, 8));

    private static void ChangeState(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc503DriverDto current, string actor, int next)
    {
        using var command = new NpgsqlCommand("""
            UPDATE "YMES".tblqcinspdriver
            SET issuestate=@next,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
            WHERE lower(inspdriverno)=lower(@no) AND issuestate=@state
              AND datastamp IS NOT DISTINCT FROM @stamp;
            """, connection, transaction);
        AddText(command, "no", current.InspDriverNo, 25);
        AddNumeric(command, "next", next);
        BindExpected(command, current, actor);
        if (command.ExecuteNonQuery() != 1) throw Conflict();
    }

    private static void EnsureNoReferences(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        // Only fixed source table names are interpolated. Referenced Drivers
        // may be retired (state 2 -> -1) but cannot be physically removed.
        foreach (var table in new[] { "tblqcinspmachineparametertype", "tblqcinspmachineparameter", "tblqcinspmachinebasis" })
        {
            if (!TableExists(connection, transaction, table)) continue;
            using var command = new NpgsqlCommand($"SELECT 1 FROM \"YMES\".{table} WHERE lower(inspdriverno)=lower(@no) LIMIT 1;", connection, transaction);
            AddText(command, "no", no, 25);
            if (command.ExecuteScalar() is not null)
                throw Rule(409, "此檢測 Driver 已被儀器類別或儀器資料引用，不能實體刪除。");
        }
    }

    private sealed record DriverValue(string No, string Function, string? Description);
    private static DriverValue Normalize(Qc503DriverWriteRequest request)
    {
        var no = NormalizeKey(request.InspDriverNo);
        var function = (request.FunctionName ?? "").Trim();
        if (function.Length == 0 || function.Length > 50 || !AllowedFunctions.Contains(function, StringComparer.Ordinal))
            throw Rule(400, "FunctionName 必須選擇 funDrv_Thick、funDrv_Channel 或 funDrFunctionC。");
        var description = (request.Description ?? "").Trim();
        if (description.Length > 255 || description.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t')))
            throw Rule(400, "Description 不可超過 255 個字元或含非換行控制字元。");
        return new(no, function, description.Length == 0 ? null : description);
    }

    private static string NormalizeKey(string? key)
    {
        var no = (key ?? "").Trim().ToUpperInvariant();
        if (no.Length == 0 || no.Length > 25 || no.Any(char.IsControl))
            throw Rule(400, "InspDriverNo 為必填，且不可超過 25 個字元或含控制字元。");
        return no;
    }

    private static string NormalizeActor(string actor)
    {
        actor = actor.Trim();
        if (actor.Length == 0 || actor.Length > 10 || actor.Any(char.IsControl))
            throw Rule(401, "登入帳號不可空白且不可超過 10 個字元。");
        return actor;
    }

    private static void EnsureExpected(Qc503DriverDto current, int state, decimal? stamp)
    {
        ValidateState(state);
        if (current.IssueState != state || current.DataStamp != stamp) throw Conflict();
    }

    private static void ValidateState(int state)
    {
        if (state is not (-1 or 0 or 1 or 2)) throw Rule(400, "IssueState 僅允許 -1、0、1、2。");
    }

    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@table AND c.relkind IN ('r','p','v','m','f'));", connection, transaction);
        AddText(command, "table", table, 63);
        return command.ExecuteScalar() is true;
    }

    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction)
    {
        if (!TableExists(connection, transaction, "tblqcinspdriver"))
            throw Rule(503, "YMES 尚未建立 TBLQCINSPDRIVER（QC501-QC503 共用 migration 0027）。");
    }

    private static void Bind(NpgsqlCommand command, DriverValue value)
    {
        AddText(command, "no", value.No, 25);
        AddText(command, "function", value.Function, 50);
        AddText(command, "description", value.Description, 255);
    }

    private static void BindExpected(NpgsqlCommand command, Qc503DriverDto current, string actor)
    {
        AddText(command, "actor", actor, 10);
        AddNumeric(command, "state", current.IssueState);
        AddNumeric(command, "stamp", current.DataStamp);
    }

    private static void AddText(NpgsqlCommand command, string name, string? value, int size)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Varchar, size);
        parameter.Value = (object?)value ?? DBNull.Value;
    }

    private static void AddNumeric(NpgsqlCommand command, string name, decimal? value)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Numeric);
        parameter.Value = (object?)value ?? DBNull.Value;
    }

    private static string? NullableText(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static DateTime? NullableDate(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    private static void Rollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { /* Preserve original error. */ } }
    private static Qc503RuleException Rule(int status, string message) => new(status, message);
    private static Qc503RuleException Conflict() => Rule(409, "資料已被其他作業修改，請重新查詢後再試。");
}
