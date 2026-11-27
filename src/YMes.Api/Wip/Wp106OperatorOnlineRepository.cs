using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed class Wp106OperatorOnlineRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 100_000;
    private const int MaxPageSize = 100;
    private const int MaxPlaceCount = 100;

    public async Task<Wp106OperatorOnlinePage> ListAsync(Wp106OperatorOnlineQuery query, CancellationToken cancellationToken)
    {
        var filter = Normalize(query);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction))
            await readOnly.ExecuteNonQueryAsync(cancellationToken);

        var offset = checked((filter.Page - 1) * filter.PageSize);
        await using var command = new NpgsqlCommand("""
            SELECT s.userno, COALESCE(NULLIF(btrim(u.username), ''), s.userno), s.shiftno,
                   s.logindate, s.workdate, s.opno, s.multioperatormode::int, s.loginplaceno
            FROM "YMES".tblwipoperatorstate AS s
            LEFT JOIN "YMES".tblusruserbasis AS u ON u.userno = s.userno
            WHERE @hasPlaces = false OR upper(btrim(s.loginplaceno)) = ANY(@placeNos)
            ORDER BY s.logindate, s.userno COLLATE "C", s.multioperatormode, s.loginplaceno COLLATE "C"
            LIMIT @take OFFSET @offset;
            """, connection, transaction);
        command.Parameters.Add("hasPlaces", NpgsqlDbType.Boolean).Value = filter.PlaceNos.Length != 0;
        command.Parameters.Add("placeNos", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = filter.PlaceNos;
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = filter.PageSize + 1;
        command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = offset;

        var rows = new List<Wp106OperatorOnlineRow>(filter.PageSize + 1);
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(new Wp106OperatorOnlineRow(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2),
                    DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Unspecified),
                    DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Unspecified),
                    reader.GetString(5), reader.GetInt32(6), reader.GetString(7)));
        }

        var hasMore = rows.Count > filter.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        await transaction.CommitAsync(cancellationToken);
        return new(filter.Page, filter.PageSize, hasMore, rows);
    }

    private static NormalizedQuery Normalize(Wp106OperatorOnlineQuery query)
    {
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 50;
        if (page is < 1 or > MaxPage) throw Bad($"頁碼必須介於 1 到 {MaxPage:N0}。");
        if (pageSize is < 1 or > MaxPageSize) throw Bad($"每頁筆數必須介於 1 到 {MaxPageSize}。");
        if (query.PlaceNos is { Length: > MaxPlaceCount }) throw Bad($"LoginPlaceNo 最多 {MaxPlaceCount} 個。");

        var places = (query.PlaceNos ?? [])
            .Select(value => Text(value))
            .Where(value => value is not null)
            .Select(value => value!.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (places.Length > MaxPlaceCount) throw Bad($"LoginPlaceNo 最多 {MaxPlaceCount} 個。");
        return new(page, pageSize, places);
    }

    private static string? Text(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        try { _ = new UTF8Encoding(false, true).GetByteCount(normalized); }
        catch (EncoderFallbackException) { throw Bad("LoginPlaceNo 含無效 Unicode。"); }
        if (normalized.Contains('\0') || normalized.Any(char.IsControl) || normalized.EnumerateRunes().Count() > 50)
            throw Bad("LoginPlaceNo 長度或字元不正確（最多 50 字元）。");
        return normalized;
    }

    private static Wp106OperatorOnlineRuleException Bad(string message) => new(400, message);
    private sealed record NormalizedQuery(int Page, int PageSize, string[] PlaceNos);
}

public sealed class Wp106OperatorOnlineRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class Wp106OperatorOnlineQuery
{
    public string[]? PlaceNos { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record Wp106OperatorOnlineRow(string UserNo, string UserName, string ShiftNo,
    DateTime LoginDate, DateTime WorkDate, string OpNo, int MultiOperatorMode, string LoginPlaceNo);

public sealed record Wp106OperatorOnlinePage(int Page, int PageSize, bool HasMore,
    IReadOnlyList<Wp106OperatorOnlineRow> Items);
