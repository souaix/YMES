using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc101Repository(NpgsqlDataSource dataSource)
{
    private const string Columns = "samplingtable,minnum,maxnum,samplenum,rejectnum,creator,createdate,reviser,revisedate,datastamp";
    private const decimal RangeLimit = 99999999.9999m;

    public IReadOnlyList<Qc101SamplingTableDto> List(string? searchFieldName, string? searchFieldValue)
    {
        var field = string.IsNullOrWhiteSpace(searchFieldName) ? "SAMPLINGTABLE" : searchFieldName.Trim().ToUpperInvariant();
        // Explicit YMES contract matching the Vue display: literal,
        // case-insensitive contains. Numeric text has no trailing scale zeros.
        // This does not claim to reproduce the unverified legacy XML helper.
        // Only these fixed SQL expressions can become a search column.
        var expression = field switch
        {
            "SAMPLINGTABLE" => "samplingtable",
            "MINNUM" => "(CASE WHEN minnum=trunc(minnum) THEN trunc(minnum)::text ELSE rtrim(minnum::text,'0') END)",
            "MAXNUM" => "(CASE WHEN maxnum=trunc(maxnum) THEN trunc(maxnum)::text ELSE rtrim(maxnum::text,'0') END)",
            _ => throw Rule(400, "搜尋欄位僅允許 SamplingTable、MinNum、MaxNum。")
        };
        var query = (searchFieldValue ?? "").Trim();
        if (query.Length > 128 || query.Any(char.IsControl)) throw Rule(400, "查詢值最長 128 個字元且不可含控制字元。");
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null);
        using var command = new NpgsqlCommand($"""
            SELECT {Columns} FROM "YMES".tblqcsamplingtable
            WHERE @query='' OR position(lower(@query) in lower({expression}))>0
            ORDER BY samplingtable,minnum,maxnum;
            """, connection);
        Text(command, "query", query, 128);
        var result = new List<Qc101SamplingTableDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(Read(reader));
        return result;
    }

    public Qc101SamplingTableDto Create(Qc101SamplingTableCreateRequest request, string actor)
    {
        actor = NormalizeActor(actor);
        var key = NormalizeKey(request.SamplingTable, request.MinNum, request.MaxNum);
        var values = NormalizeValues(request);
        return Insert(key, values, actor, false) ?? throw Duplicate();
    }

    public Qc101SamplingTableDto Update(string samplingTable, decimal minNum, decimal maxNum, decimal? dataStamp,
        Qc101SamplingTableValuesRequest request, string actor)
    {
        actor = NormalizeActor(actor);
        var key = NormalizeKey(samplingTable, minNum, maxNum);
        var values = NormalizeValues(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = ReadForUpdate(connection, transaction, key);
            if (current.DataStamp != dataStamp) throw Conflict();
            using var command = new NpgsqlCommand($"""
                UPDATE "YMES".tblqcsamplingtable
                SET samplenum=@sample,rejectnum=@reject,reviser=@actor,revisedate=now(),
                    datastamp=COALESCE(datastamp,0)+1
                WHERE samplingtable=@table AND minnum=@min AND maxnum=@max
                  AND datastamp IS NOT DISTINCT FROM @stamp
                RETURNING {Columns};
                """, connection, transaction);
            BindKey(command, key with { Table = current.SamplingTable });
            BindValues(command, values);
            Text(command, "actor", actor, 10);
            Number(command, "stamp", dataStamp);
            var result = ReadReturned(command);
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc101SamplingTableDto Delete(string samplingTable, decimal minNum, decimal maxNum, decimal? dataStamp, string actor)
    {
        NormalizeActor(actor);
        var key = NormalizeKey(samplingTable, minNum, maxNum);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = ReadForUpdate(connection, transaction, key);
            if (current.DataStamp != dataStamp) throw Conflict();
            using var command = new NpgsqlCommand($"""
                DELETE FROM "YMES".tblqcsamplingtable
                WHERE samplingtable=@table AND minnum=@min AND maxnum=@max
                  AND datastamp IS NOT DISTINCT FROM @stamp
                RETURNING {Columns};
                """, connection, transaction);
            BindKey(command, key with { Table = current.SamplingTable });
            Number(command, "stamp", dataStamp);
            var result = ReadReturned(command);
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc101CopyResult Copy(Qc101CopyRequest request, string actor)
    {
        actor = NormalizeActor(actor);
        var source = TableName(request.SourceSamplingTable);
        var target = TableName(request.TargetSamplingTable);
        var sourceRows = ReadSource(source);
        if (sourceRows.Count == 0) throw Rule(404, "來源 SamplingTable 沒有可複製的區間。");
        var inserted = 0;
        var skipped = 0;
        // The legacy CopyTable performs separate Add calls. Each destination
        // row commits independently; a later failure preserves earlier rows.
        foreach (var row in sourceRows)
        {
            try
            {
                var key = NormalizeKey(target, row.MinNum, row.MaxNum);
                var values = NormalizeValues(new Qc101SamplingTableValuesRequest { SampleNum = row.SampleNum, RejectNum = row.RejectNum });
                if (Insert(key, values, actor, true) is null) skipped++;
                else inserted++;
            }
            catch (Qc101RuleException ex)
            {
                return new(inserted, skipped, $"區間 {row.MinNum}–{row.MaxNum} 複製失敗：{ex.Message}");
            }
            catch (NpgsqlException)
            {
                return new(inserted, skipped, $"區間 {row.MinNum}–{row.MaxNum} 複製時資料庫操作失敗，後續區間未處理；先前成功資料已保留。");
            }
        }
        return new(inserted, skipped);
    }

    private Qc101SamplingTableDto? Insert(SamplingKey key, SamplingValues values, string actor, bool skipDuplicate)
    {
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            using (var duplicate = new NpgsqlCommand("""
                SELECT 1 FROM "YMES".tblqcsamplingtable
                WHERE lower(samplingtable)=lower(@table) AND minnum=@min AND maxnum=@max;
                """, connection, transaction))
            {
                BindKey(duplicate, key);
                if (duplicate.ExecuteScalar() is not null)
                {
                    if (!skipDuplicate) throw Duplicate();
                    transaction.Commit();
                    return null;
                }
            }
            // Let the verified source DEFAULT provide DataStamp=0. Audit is
            // always generated from the authenticated user/server clock.
            using var command = new NpgsqlCommand($"""
                INSERT INTO "YMES".tblqcsamplingtable
                    (samplingtable,minnum,maxnum,samplenum,rejectnum,creator,createdate)
                VALUES (@table,@min,@max,@sample,@reject,@actor,now())
                RETURNING {Columns};
                """, connection, transaction);
            BindKey(command, key);
            BindValues(command, values);
            Text(command, "actor", actor, 10);
            var result = ReadReturned(command);
            transaction.Commit();
            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Rollback(transaction);
            // A concurrent copy/create may have inserted the same destination
            // composite key after our existence check. That is still a skip.
            if (skipDuplicate) return null;
            throw Duplicate();
        }
        catch { Rollback(transaction); throw; }
    }

    private IReadOnlyList<Qc101SamplingTableDto> ReadSource(string source)
    {
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null);
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblqcsamplingtable WHERE lower(samplingtable)=lower(@table) ORDER BY minnum,maxnum;", connection);
        Text(command, "table", source, 25);
        var result = new List<Qc101SamplingTableDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(Read(reader));
        return result;
    }

    private static Qc101SamplingTableDto ReadForUpdate(NpgsqlConnection connection, NpgsqlTransaction transaction, SamplingKey key)
    {
        using var command = new NpgsqlCommand($"""
            SELECT {Columns} FROM "YMES".tblqcsamplingtable
            WHERE lower(samplingtable)=lower(@table) AND minnum=@min AND maxnum=@max FOR UPDATE;
            """, connection, transaction);
        BindKey(command, key);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到指定 SamplingTable、MinNum、MaxNum 的抽樣表格。");
        var row = Read(reader);
        if (reader.Read()) throw Rule(409, "抽樣表格複合鍵存在名稱大小寫重複資料，請先修正資料。");
        return row;
    }

    private static Qc101SamplingTableDto ReadReturned(NpgsqlCommand command)
    {
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Conflict();
        var row = Read(reader);
        if (reader.Read()) throw Rule(409, "抽樣表格複合鍵不是唯一資料，本次操作已取消。");
        return row;
    }

    private static Qc101SamplingTableDto Read(NpgsqlDataReader r) => new(
        r.GetString(0), r.GetDecimal(1), r.GetDecimal(2), r.GetDecimal(3), DecimalOrNull(r, 4),
        TextOrNull(r, 5), DateOrNull(r, 6), TextOrNull(r, 7), DateOrNull(r, 8), DecimalOrNull(r, 9));

    private sealed record SamplingKey(string Table, decimal Min, decimal Max);
    private sealed record SamplingValues(decimal Sample, decimal? Reject);

    private static SamplingKey NormalizeKey(string? table, decimal? minNum, decimal? maxNum)
    {
        var name = TableName(table);
        var min = RangeNumber(minNum, "MinNum");
        var max = RangeNumber(maxNum, "MaxNum");
        if (min > max) throw Rule(400, "MinNum 不得大於 MaxNum。");
        return new(name, min, max);
    }

    private static SamplingValues NormalizeValues(Qc101SamplingTableValuesRequest request)
    {
        if (!request.SampleNum.HasValue || request.SampleNum.Value < 0 || request.SampleNum.Value > 99999999m
            || decimal.Truncate(request.SampleNum.Value) != request.SampleNum.Value)
            throw Rule(400, "SampleNum 為必填，且必須是 0 到 99999999 的整數。");
        if (request.RejectNum.HasValue && (request.RejectNum.Value < 0 || request.RejectNum.Value > request.SampleNum.Value
            || decimal.Truncate(request.RejectNum.Value) != request.RejectNum.Value))
            throw Rule(400, "RejectNum 可空白；有值時必須為非負整數且不得大於 SampleNum。");
        return new(request.SampleNum.Value, request.RejectNum);
    }

    private static decimal RangeNumber(decimal? value, string field)
    {
        if (!value.HasValue || value.Value < -RangeLimit || value.Value > RangeLimit || decimal.Round(value.Value, 4) != value.Value)
            throw Rule(400, $"{field} 為必填，範圍為 -99999999.9999 到 99999999.9999，最多 4 位小數。");
        return value.Value;
    }

    private static string TableName(string? table)
    {
        var name = (table ?? "").Trim().ToUpperInvariant();
        if (name.Length == 0 || name.Length > 25 || name.Any(char.IsControl))
            throw Rule(400, "SamplingTable 為必填，最長 25 個字元且不可含控制字元。");
        return name;
    }

    private static string NormalizeActor(string? actor)
    {
        var value = (actor ?? "").Trim();
        if (value.Length == 0 || value.Length > 10 || value.Any(char.IsControl))
            throw Rule(401, "登入帳號必須有效，且不可超過 10 個字元。");
        return value;
    }

    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='YMES' AND table_name='tblqcsamplingtable');", connection, transaction);
        if (command.ExecuteScalar() is not true) throw Rule(503, "YMES 尚未建立 TBLQCSAMPLINGTABLE，請先完成 QC101 migration。");
    }

    private static void BindKey(NpgsqlCommand command, SamplingKey key)
    {
        Text(command, "table", key.Table, 25);
        Number(command, "min", key.Min);
        Number(command, "max", key.Max);
    }

    private static void BindValues(NpgsqlCommand command, SamplingValues values)
    {
        Number(command, "sample", values.Sample);
        Number(command, "reject", values.Reject);
    }

    private static void Text(NpgsqlCommand command, string name, string? value, int size) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand command, string name, decimal? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static string? TextOrNull(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static decimal? DecimalOrNull(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    private static DateTime? DateOrNull(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    private static void Rollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { /* Preserve original failure. */ } }
    private static Qc101RuleException Rule(int status, string message) => new(status, message);
    private static Qc101RuleException Duplicate() => Rule(409, "相同 SamplingTable、MinNum、MaxNum 的抽樣表格已存在。");
    private static Qc101RuleException Conflict() => Rule(409, "抽樣表格已被其他作業修改，請重新載入後再試。");
}
