using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed class Wp507FutureWaitRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 10_000;
    private const int MaxPageSize = 100;

    public async Task<Wp507FutureWaitPage> ListAsync(
        Wp507FutureWaitQuery query, CancellationToken cancellationToken)
    {
        var lotNo = NormalizeText(query.LotNo, 50, "LotNo")?.ToUpperInvariant();
        var opNo = NormalizeText(query.OpNo, 20, "OPNo")?.ToUpperInvariant();
        if (query.WaitTime is not null and not (0 or 1))
            throw Bad(400, "WaitTime 僅接受 0（Start）或 1（End）。");
        if (lotNo is null && opNo is null && query.WaitTime is null)
            throw Bad(400, "請至少輸入 Lot No、OP No 或選擇 Wait Time 一項查詢條件。");

        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 50;
        if (page is < 1 or > MaxPage) throw Bad(400, $"頁碼必須介於 1 到 {MaxPage:N0}。");
        if (pageSize is < 1 or > MaxPageSize) throw Bad(400, $"每頁筆數必須介於 1 到 {MaxPageSize}。");
        var offset = checked((page - 1) * pageSize);

        var predicates = new List<string>();
        if (lotNo is not null) predicates.Add("upper(btrim(f.lotno))=@lotNo");
        if (opNo is not null) predicates.Add("upper(btrim(f.opno))=@opNo");
        if (query.WaitTime is not null) predicates.Add("f.waittime=@waitTime::numeric");

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY; SET LOCAL lock_timeout='2s'; SET LOCAL statement_timeout='10s';", connection, transaction))
            await readOnly.ExecuteNonQueryAsync(cancellationToken);

        var rows = new List<Wp507FutureWaitRow>(pageSize + 1);
        await using (var command = new NpgsqlCommand($"""
            SELECT f.lotno,f.opno,f.waittime::integer,f.description,f.creator,f.createdate
            FROM "YMES".tblwipfuturewait AS f
            WHERE {string.Join(" AND ", predicates)}
            ORDER BY f.lotno COLLATE "C",f.opno COLLATE "C",f.waittime
            LIMIT @take OFFSET @offset;
            """, connection, transaction))
        {
            if (lotNo is not null) command.Parameters.Add("lotNo", NpgsqlDbType.Varchar).Value = lotNo;
            if (opNo is not null) command.Parameters.Add("opNo", NpgsqlDbType.Varchar).Value = opNo;
            if (query.WaitTime is not null) command.Parameters.Add("waitTime", NpgsqlDbType.Integer).Value = query.WaitTime.Value;
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = pageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = offset;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new(
                    reader.GetString(0), reader.GetString(1), reader.GetInt32(2),
                    NullableString(reader, 3), NullableString(reader, 4),
                    reader.IsDBNull(5) ? null : DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Unspecified)));
            }
        }

        var hasMore = rows.Count > pageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        await transaction.CommitAsync(cancellationToken);
        return new(page, pageSize, hasMore, rows);
    }

    public async Task<Wp507FutureWaitPageOf<Wp507OpOption>> LookupOperationsAsync(
        Wp507OpLookupQuery query, CancellationToken cancellationToken)
    {
        var keyword = NormalizeText(query.Keyword, 20, "Keyword")?.ToUpperInvariant();
        if (keyword is null) throw Bad(400, "請輸入至少一個 OP No 字元再查找。");
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 50;
        if (page is < 1 or > MaxPage) throw Bad(400, $"頁碼必須介於 1 到 {MaxPage:N0}。");
        if (pageSize is < 1 or > MaxPageSize) throw Bad(400, $"每頁筆數必須介於 1 到 {MaxPageSize}。");
        var offset = checked((page - 1) * pageSize);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY; SET LOCAL lock_timeout='2s'; SET LOCAL statement_timeout='10s';", connection, transaction))
            await readOnly.ExecuteNonQueryAsync(cancellationToken);

        var rows = new List<Wp507OpOption>(pageSize + 1);
        await using (var command = new NpgsqlCommand("""
            SELECT o.opno,o.opname
            FROM "YMES".tblopbasis AS o
            WHERE strpos(upper(btrim(o.opno)),@keyword)>0
            ORDER BY o.opno COLLATE "C"
            LIMIT @take OFFSET @offset;
            """, connection, transaction))
        {
            command.Parameters.Add("keyword", NpgsqlDbType.Varchar).Value = keyword;
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = pageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = offset;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) rows.Add(new(reader.GetString(0), reader.GetString(1)));
        }

        var hasMore = rows.Count > pageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        await transaction.CommitAsync(cancellationToken);
        return new(page, pageSize, hasMore, rows);
    }

    private static string? NormalizeText(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        try { _ = new UTF8Encoding(false, true).GetByteCount(normalized); }
        catch (EncoderFallbackException) { throw Bad(400, $"{field} 含無效 Unicode。"); }
        if (normalized.Contains('\0') || normalized.Any(char.IsControl) || normalized.EnumerateRunes().Count() > maxLength)
            throw Bad(400, $"{field} 長度或字元不正確（最多 {maxLength} 字元）。");
        return normalized;
    }

    private static string? NullableString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static Wp507FutureWaitRuleException Bad(int status, string message) => new(status, message);
}
