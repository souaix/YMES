using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc102Repository(NpgsqlDataSource dataSource)
{
    private const string Columns = "samplingtype,minnum,maxnum,slevel,samplenum,rejectnum,kvalue,fvalue,creator,createdate,reviser,revisedate,datastamp";

    public IReadOnlyList<Qc102SamplingDto> List()
    {
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null);
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblqcsampling1916 ORDER BY samplingtype,minnum,maxnum,slevel;", connection);
        var result = new List<Qc102SamplingDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(Read(reader));
        return result;
    }

    public Qc102SamplingDto Create(Qc102SamplingCreateRequest request, string actor)
    {
        actor = NormalizeActor(actor);
        var key = NormalizeKey(request.SamplingType, request.MinNum, request.MaxNum, request.SLevel);
        var values = NormalizeValues(key.Type, request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            using (var duplicate = new NpgsqlCommand("""
                SELECT 1 FROM "YMES".tblqcsampling1916
                WHERE samplingtype=@type AND minnum=@min AND maxnum=@max AND lower(slevel)=lower(@level);
                """, connection, transaction))
            {
                BindKey(duplicate, key);
                if (duplicate.ExecuteScalar() is not null) throw Duplicate();
            }
            using var command = new NpgsqlCommand($"""
                INSERT INTO "YMES".tblqcsampling1916
                    (samplingtype,minnum,maxnum,slevel,samplenum,rejectnum,kvalue,fvalue,creator,createdate,datastamp)
                VALUES (@type,@min,@max,@level,@sample,@reject,@k,@f,@actor,now(),0)
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
            throw Duplicate();
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc102SamplingDto Update(string samplingType, decimal minNum, decimal maxNum, string sLevel,
        decimal? dataStamp, Qc102SamplingValuesRequest request, string actor)
    {
        actor = NormalizeActor(actor);
        var key = NormalizeKey(samplingType, minNum, maxNum, sLevel);
        var values = NormalizeValues(key.Type, request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = ReadForUpdate(connection, transaction, key);
            if (current.DataStamp != dataStamp) throw Conflict();
            using var command = new NpgsqlCommand($"""
                UPDATE "YMES".tblqcsampling1916
                SET samplenum=@sample,rejectnum=@reject,kvalue=@k,fvalue=@f,
                    reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE samplingtype=@type AND minnum=@min AND maxnum=@max
                  AND slevel=@level AND datastamp IS NOT DISTINCT FROM @stamp
                RETURNING {Columns};
                """, connection, transaction);
            // Preserve the original source key's spelling if a legacy row was
            // lowercase. The route is normalized only for locating the row.
            BindKey(command, key with { Level = current.SLevel });
            BindValues(command, values);
            Text(command, "actor", actor, 10);
            Number(command, "stamp", dataStamp);
            var result = ReadReturned(command);
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc102SamplingDto Delete(string samplingType, decimal minNum, decimal maxNum, string sLevel,
        decimal? dataStamp, string actor)
    {
        NormalizeActor(actor);
        var key = NormalizeKey(samplingType, minNum, maxNum, sLevel);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = ReadForUpdate(connection, transaction, key);
            if (current.DataStamp != dataStamp) throw Conflict();
            using var command = new NpgsqlCommand($"""
                DELETE FROM "YMES".tblqcsampling1916
                WHERE samplingtype=@type AND minnum=@min AND maxnum=@max
                  AND slevel=@level AND datastamp IS NOT DISTINCT FROM @stamp
                RETURNING {Columns};
                """, connection, transaction);
            BindKey(command, key with { Level = current.SLevel });
            Number(command, "stamp", dataStamp);
            var result = ReadReturned(command);
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    private static Qc102SamplingDto ReadForUpdate(NpgsqlConnection connection, NpgsqlTransaction transaction, SamplingKey key)
    {
        using var command = new NpgsqlCommand($"""
            SELECT {Columns} FROM "YMES".tblqcsampling1916
            WHERE samplingtype=@type AND minnum=@min AND maxnum=@max
              AND lower(slevel)=lower(@level) FOR UPDATE;
            """, connection, transaction);
        BindKey(command, key);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到指定 SamplingType、MinNum、MaxNum、SLevel 的抽樣計劃。");
        var row = Read(reader);
        if (reader.Read()) throw Rule(409, "抽樣計劃複合鍵存在 SLevel 大小寫重複資料，請先修正資料。");
        return row;
    }

    private static Qc102SamplingDto ReadReturned(NpgsqlCommand command)
    {
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Conflict();
        var row = Read(reader);
        if (reader.Read()) throw Rule(409, "抽樣計劃複合鍵不是唯一資料，本次操作已取消。");
        return row;
    }

    private static Qc102SamplingDto Read(NpgsqlDataReader r) => new(
        r.GetString(0), Convert.ToInt32(r.GetValue(1)), Convert.ToInt32(r.GetValue(2)), r.GetString(3),
        r.IsDBNull(4) ? null : Convert.ToInt32(r.GetValue(4)), r.IsDBNull(5) ? null : Convert.ToInt32(r.GetValue(5)),
        DecimalOrNull(r, 6), DecimalOrNull(r, 7), TextOrNull(r, 8), DateOrNull(r, 9),
        TextOrNull(r, 10), DateOrNull(r, 11), DecimalOrNull(r, 12));

    private sealed record SamplingKey(string Type, int Min, int Max, string Level);
    private sealed record SamplingValues(int Sample, int Reject, decimal? K, decimal? F);

    private static SamplingKey NormalizeKey(string? samplingType, decimal? minNum, decimal? maxNum, string? sLevel)
    {
        var type = (samplingType ?? "").Trim();
        if (type is not ("0" or "1")) throw Rule(400, "SamplingType 必須為 0（Attributes）或 1（Variables）。");
        var level = (sLevel ?? "").Trim().ToUpperInvariant();
        if (level.Length == 0 || level.Length > 6 || level.Any(char.IsControl))
            throw Rule(400, "SLevel 為必填，最長 6 個字元且不可含控制字元。");
        var min = Integer(minNum, "MinNum", -99999999m, 99999999m);
        var max = Integer(maxNum, "MaxNum", -99999999m, 99999999m);
        if (min > max) throw Rule(400, "MinNum 不得大於 MaxNum。");
        return new(type, min, max, level);
    }

    private static SamplingValues NormalizeValues(string type, Qc102SamplingValuesRequest request)
    {
        var sample = Integer(request.SampleNum, "SampleNum", 0, 999999);
        var reject = Integer(request.RejectNum, "RejectNum", 0, 999999);
        if (reject > sample) throw Rule(400, "RejectNum 不得大於 SampleNum。");
        // Attribute sampling does not use variable-mode coefficients.
        if (type == "0") return new(sample, reject, null, null);
        return new(sample, reject, Coefficient(request.KValue, "KValue"), Coefficient(request.FValue, "FValue"));
    }

    private static int Integer(decimal? value, string field, decimal min, decimal max)
    {
        if (!value.HasValue || value.Value != decimal.Truncate(value.Value) || value.Value < min || value.Value > max)
            throw Rule(400, $"{field} 為必填，且必須是 {min} 到 {max} 的整數。");
        return decimal.ToInt32(value.Value);
    }

    private static decimal Coefficient(decimal? value, string field)
    {
        if (!value.HasValue || value.Value < -9999999.999m || value.Value > 9999999.999m || decimal.Round(value.Value, 3) != value.Value)
            throw Rule(400, $"{field} 為 Variables 必填，範圍為 -9999999.999 到 9999999.999，最多 3 位小數。");
        return value.Value;
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
        using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='YMES' AND table_name='tblqcsampling1916');", connection, transaction);
        if (command.ExecuteScalar() is not true)
            throw Rule(503, "YMES 尚未建立 TBLQCSAMPLING1916，請先完成 QC102 migration。");
    }

    private static void BindKey(NpgsqlCommand command, SamplingKey key)
    {
        Text(command, "type", key.Type, 25);
        Number(command, "min", key.Min);
        Number(command, "max", key.Max);
        Text(command, "level", key.Level, 6);
    }

    private static void BindValues(NpgsqlCommand command, SamplingValues values)
    {
        Number(command, "sample", values.Sample);
        Number(command, "reject", values.Reject);
        Number(command, "k", values.K);
        Number(command, "f", values.F);
    }

    private static void Text(NpgsqlCommand command, string name, string? value, int size) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand command, string name, decimal? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static string? TextOrNull(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static decimal? DecimalOrNull(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    private static DateTime? DateOrNull(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    private static void Rollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { /* Preserve original failure. */ } }
    private static Qc102RuleException Rule(int status, string message) => new(status, message);
    private static Qc102RuleException Duplicate() => Rule(409, "相同 SamplingType、MinNum、MaxNum、SLevel 的抽樣計劃已存在。");
    private static Qc102RuleException Conflict() => Rule(409, "抽樣計劃已被其他作業修改，請重新載入後再試。");
}
