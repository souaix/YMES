using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Enterprise;

public sealed class Et202Repository(NpgsqlDataSource dataSource)
{
    private const string Table = "\"YMES\".tblentositem";
    private const string Columns = "ositemno,ositemname,plandays,processno,returntype,description,creator,createdate,datastamp,reviser,revisedate";
    private const decimal MaxStamp = 99_999_999_999m;

    public IReadOnlyList<Et202OsItemDto> List(string? osItemNo)
    {
        var search = osItemNo is null or "" ? null : Input(osItemNo, "OSItemNo查詢", 2, required: false, key: true);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        SetReadOnly(connection, transaction); EnsureOwn(connection, transaction);
        using var command = new NpgsqlCommand($"""
            WITH source_rows AS (
                SELECT *,count(*) OVER(PARTITION BY lower(btrim(ositemno))) AS aliases FROM {Table})
            SELECT {Columns},aliases FROM source_rows
            WHERE @search IS NULL OR position(lower(@search) in lower(ositemno))>0
            ORDER BY ositemno COLLATE "C";
            """, connection, transaction);
        Text(command, "search", search);
        var rows = new List<Et202OsItemDto>();
        using (var reader = command.ExecuteReader()) while (reader.Read())
        {
            if (reader.GetInt64(11) != 1) throw Alias();
            rows.Add(Map(reader));
        }
        transaction.Commit(); return rows;
    }

    public Et202OsItemDto Get(string osItemNo)
    {
        var no = Key(osItemNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        SetReadOnly(connection, transaction); EnsureOwn(connection, transaction);
        var saved = Read(connection, transaction, no);
        transaction.Commit(); return saved;
    }

    public Et202OsItemDto Create(Et202WriteRequest request, string actor)
    {
        Validate(request); actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        // Parent namespace and SHARE precede the child namespace / row locks.
        var processNo = Et202ProcessReferenceGuard.LockQualifyingProcess(connection, transaction, request.ProcessNo!);
        var no = NewKey(connection, transaction, request.OsItemNo!);
        Acquire(connection, transaction, no); EnsureOwn(connection, transaction, write: true);
        using (var duplicate = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {Table} WHERE lower(btrim(ositemno))=lower(btrim(@no)));", connection, transaction))
        { Text(duplicate, "no", no); if (duplicate.ExecuteScalar() is true) throw Alias(); }
        using var command = new NpgsqlCommand($"""
            INSERT INTO {Table}(ositemno,ositemname,plandays,processno,returntype,description,creator,createdate,datastamp,reviser,revisedate)
            VALUES(@no,@name,@days,@process,@return,@description,@actor,now() AT TIME ZONE 'UTC',0,NULL,NULL)
            RETURNING {Columns};
            """, connection, transaction);
        Bind(command, no, processNo, request, actor);
        var saved = Returned(command); transaction.Commit(); return saved;
    }

    public Et202OsItemDto Update(string osItemNo, Et202UpdateRequest request, string actor)
    {
        var no = Key(osItemNo); Validate(request); Stamp(request.DataStamp); actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        // The native editor rechecks its approved outsourced-process list on
        // every save. Do not invent a CurVersion-only or retired-parent bypass.
        var qualifiedProcess = Et202ProcessReferenceGuard.LockQualifyingProcess(connection, transaction, request.ProcessNo!);
        Acquire(connection, transaction, no); EnsureOwn(connection, transaction, write: true);
        var current = Read(connection, transaction, no, update: true);
        if (!string.Equals(request.OsItemNo, current.OsItemNo, StringComparison.Ordinal))
            throw Rule(422, "修改時不可變更原始OSItemNo（包含大小寫與空白）。");
        if (current.DataStamp != request.DataStamp) throw Conflict();
        Incrementable(current.DataStamp);
        // Preserve the unchanged legacy raw parent representation; changed
        // references adopt the actual qualified parent's canonical raw value.
        var processNo = string.Equals(request.ProcessNo, current.ProcessNo, StringComparison.Ordinal)
            ? current.ProcessNo : qualifiedProcess;
        using var command = new NpgsqlCommand($"""
            UPDATE {Table} SET ositemname=@name,plandays=@days,processno=@process,
                returntype=@return,description=@description,reviser=@actor,
                revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE ositemno=@no AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {Columns};
            """, connection, transaction);
        Bind(command, current.OsItemNo, processNo, request, actor); Number(command, "stamp", request.DataStamp);
        var saved = Returned(command); transaction.Commit(); return saved;
    }

    public Et202DeleteResult Delete(string osItemNo, Et202ExpectedRequest request, string actor)
    {
        var no = Key(osItemNo); Stamp(request.DataStamp); _ = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Acquire(connection, transaction, no); EnsureOwn(connection, transaction, write: true);
        var current = Read(connection, transaction, no, update: true);
        if (current.DataStamp != request.DataStamp) throw Conflict();
        using var command = new NpgsqlCommand($"DELETE FROM {Table} WHERE ositemno=@no AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction);
        Text(command, "no", current.OsItemNo); Number(command, "stamp", request.DataStamp);
        var affected = command.ExecuteNonQuery();
        if (affected != 1) throw Conflict();
        // A hard delete does not increment and therefore accepts a matching max.
        transaction.Commit(); return new(current.OsItemNo, affected);
    }

    private static void EnsureOwn(NpgsqlConnection connection, NpgsqlTransaction transaction, bool write = false)
    {
        // ROW EXCLUSIVE permits ordinary concurrent DML, but conflicts with
        // CREATE TRIGGER / foreign-key DDL. A fresh no-trigger/no-FK guard must
        // remain true until this writer commits, including zero-row cascades.
        if (write)
        {
            using var gate = new NpgsqlCommand($"LOCK TABLE {Table} IN ROW EXCLUSIVE MODE;", connection, transaction);
            gate.ExecuteNonQuery();
        }
        if (!Et202ProcessReferenceGuard.EnsureOptional(connection, transaction, Unavailable)) throw Unavailable();
        if (write)
        {
            // Own writes require the one native default; this is not needed
            // by the optional parent's read-only reference compatibility.
            using var defaults = new NpgsqlCommand("""
                SELECT a.attname,pg_get_expr(d.adbin,d.adrelid)
                FROM pg_catalog.pg_attrdef d JOIN pg_catalog.pg_attribute a
                  ON a.attrelid=d.adrelid AND a.attnum=d.adnum
                WHERE d.adrelid='"YMES".tblentositem'::regclass ORDER BY a.attnum;
                """, connection, transaction);
            using var reader = defaults.ExecuteReader();
            if (!reader.Read() || reader.GetString(0) != "datastamp"
                || reader.GetString(1) is not ("0" or "0::numeric" or "'0'::numeric" or "(0)::numeric")
                || reader.Read()) throw Unavailable();
        }
        // Source has no other OSItem relationship. Unknown same-semantic
        // columns fail closed, rather than silently adopting a new reference.
        using (var command = new NpgsqlCommand("""
            SELECT n.nspname,c.relname,c.relkind::text,a.attname,
                format_type(a.atttypid,a.atttypmod),a.attnotnull
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
            WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND n.nspname NOT LIKE 'pg_toast%'
              AND n.nspname NOT LIKE 'pg_temp_%' AND c.relkind IN ('r','p','v','m','f')
              AND lower(a.attname)=ANY(ARRAY['ositemno','ositem','ositemassigned'])
            ORDER BY n.nspname COLLATE "C",c.relname COLLATE "C",a.attnum;
            """, connection, transaction))
        {
            using var reader = command.ExecuteReader();
            if (!reader.Read() || reader.GetString(0) != "YMES" || reader.GetString(1) != "tblentositem"
                || reader.GetString(2) != "r" || reader.GetString(3) != "ositemno"
                || reader.GetString(4) != "character varying(2)" || !reader.GetBoolean(5) || reader.Read())
                throw Unavailable();
        }
        using var probe = new NpgsqlCommand($"SELECT {Columns} FROM {Table} WHERE false;", connection, transaction);
        probe.ExecuteNonQuery();
    }

    private static Et202OsItemDto Read(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, bool update = false)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM {Table} WHERE lower(btrim(ositemno))=lower(btrim(@no)) ORDER BY ositemno COLLATE \"C\"" + (update ? " FOR UPDATE;" : ";"), connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到指定的外包項目。");
        var row = Map(reader); if (reader.Read()) throw Alias(); return row;
    }

    private static string NewKey(NpgsqlConnection connection, NpgsqlTransaction transaction, string raw)
    {
        using var command = new NpgsqlCommand("SELECT upper(btrim(@no));", connection, transaction);
        Text(command, "no", raw); return Key((string)command.ExecuteScalar()!);
    }

    private static void Acquire(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:ENT:OSItem:' || lower(btrim(@no)),0));", connection, transaction);
        Text(command, "no", no); command.ExecuteNonQuery();
    }

    private static void Validate(Et202WriteRequest request)
    {
        _ = Key(request.OsItemNo);
        _ = Input(request.OsItemName, "OSItemName", 50, required: true);
        _ = Input(request.ProcessNo, "ProcessNo", 30, required: true, key: true);
        _ = Input(request.Description, "Description", 255, multiline: true);
        if (request.PlanDays is not (>= 0 and <= 999)) throw Rule(422, "PlanDays必須為0–999整數。");
        if (request.ReturnType is not (0 or 1)) throw Rule(422, "ReturnType只接受0或1。");
    }

    // Validate scalar counts as PostgreSQL varchar(n), without rewriting raw
    // query keys or conflating NULL with empty optional text.
    private static string? Input(string? value, string label, int maximum, bool required = false, bool key = false, bool multiline = false)
    {
        var status = key ? 400 : 422;
        if (value is null)
        { if (required) throw Rule(status, $"{label}必填。"); return null; }
        try { _ = new UTF8Encoding(false, true).GetByteCount(value); }
        catch (EncoderFallbackException) { throw Rule(status, $"{label}含無效Unicode。"); }
        if (value.EnumerateRunes().Count() > maximum || value.Any(c => char.IsControl(c) && !(multiline && c is '\r' or '\n' or '\t')))
            throw Rule(status, $"{label}最多{maximum}字元且不可含無效控制字元。");
        if (required && value.Trim(' ').Length == 0) throw Rule(status, $"{label}必填。");
        return value;
    }

    private static string Key(string? value) => Input(value, "OSItemNo", 2, required: true, key: true)!;
    private static string Actor(string value) => Input(value, "登入使用者識別碼", 10, required: true, key: true)!;
    private static void Stamp(decimal? value)
    { if (value is { } n && (n != decimal.Truncate(n) || n < -MaxStamp || n > MaxStamp)) throw Rule(400, "DataStamp須明確為NULL或numeric(11,0)範圍內整數。"); }
    private static void Incrementable(decimal? value)
    { Stamp(value); if (value == MaxStamp) throw Rule(409, "DataStamp已達來源上限；本次未變更資料。"); }
    private static void SetReadOnly(NpgsqlConnection connection, NpgsqlTransaction transaction)
    { using var command = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction); command.ExecuteNonQuery(); }
    private static Et202OsItemDto Map(NpgsqlDataReader reader) => new(reader.GetString(0), reader.GetString(1),
        decimal.ToInt32(reader.GetDecimal(2)), reader.GetString(3), decimal.ToInt32(reader.GetDecimal(4)),
        String(reader, 5), String(reader, 6), Date(reader, 7), reader.IsDBNull(8) ? null : reader.GetDecimal(8), String(reader, 9), Date(reader, 10));
    private static string? String(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
    private static DateTime? Date(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : DateTime.SpecifyKind(reader.GetDateTime(index), DateTimeKind.Utc);
    private static Et202OsItemDto Returned(NpgsqlCommand command)
    { using var reader = command.ExecuteReader(); if (!reader.Read()) throw Conflict(); var row = Map(reader); if (reader.Read()) throw Conflict(); return row; }
    private static void Bind(NpgsqlCommand command, string no, string processNo, Et202WriteRequest request, string actor)
    {
        Text(command, "no", no); Text(command, "name", request.OsItemName); Number(command, "days", request.PlanDays);
        Text(command, "process", processNo); Number(command, "return", request.ReturnType); Text(command, "description", request.Description); Text(command, "actor", actor);
    }
    private static void Text(NpgsqlCommand command, string name, string? value) => command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand command, string name, decimal? value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static Et202RuleException Rule(int status, string detail) => new(status, detail);
    private static Et202RuleException Unavailable() => Rule(503, "外包項目原生結構、權限或引用對應尚未符合契約；本次未變更資料。");
    private static Et202RuleException Alias() => Rule(409, "OSItemNo已存在或有大小寫／空白別名；原始資料保留。");
    private static Et202RuleException Conflict() => Rule(409, "外包項目版本已改變，請重新查詢；本次未保存。");
}
