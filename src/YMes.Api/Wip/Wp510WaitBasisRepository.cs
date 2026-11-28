using System.Data;
using System.Globalization;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed class Wp510WaitBasisRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 10_000;
    private const int MaxPageSize = 100;
    private const int MaxDispositionPageSize = 100;

    public async Task<Wp510Page<Wp510WaitRow>> GetWaitsAsync(
        Wp510WaitBasisQuery query, CancellationToken cancellationToken)
    {
        var lotNo = OptionalText(query.LotNo, 50, "LotNo")?.ToUpperInvariant();
        var waitNo = OptionalText(query.WaitNo, 20, "WaitNo")?.ToUpperInvariant();
        var opNo = OptionalText(query.OpNo, 20, "OPNo")?.ToUpperInvariant();
        var customerNo = OptionalText(query.CustomerNo, 50, "CustomerNo")?.ToUpperInvariant();
        var createFrom = ParseWallTime(query.CreateFrom, "CreateFrom");
        var createTo = ParseWallTime(query.CreateTo, "CreateTo");
        var releaseFrom = ParseWallTime(query.ReleaseFrom, "ReleaseFrom");
        var releaseTo = ParseWallTime(query.ReleaseTo, "ReleaseTo");
        ValidateRange(createFrom, createTo, "建立日期");
        ValidateRange(releaseFrom, releaseTo, "解除日期");
        if (lotNo is null && waitNo is null && opNo is null && customerNo is null &&
            createFrom is null && releaseFrom is null)
            throw Bad(400, "請至少提供一項有效查詢條件。");

        var page = NormalizePage(query.Page, query.PageSize);
        var predicates = new List<string> { "w.waitsource=0", "w.lotno IS NOT NULL" };
        if (lotNo is not null) predicates.Add("position(@lotNo in upper(w.lotno))>0");
        if (waitNo is not null) predicates.Add("upper(w.waitno)=@waitNo");
        if (opNo is not null) predicates.Add("upper(w.opno)=@opNo");
        if (customerNo is not null) predicates.Add("upper(w.customerno)=@customerNo");
        if (createFrom is not null) predicates.Add("w.createdate>=@createFrom");
        if (createTo is not null) predicates.Add("w.createdate<=@createTo");
        if (releaseFrom is not null) predicates.Add("w.releasedate>=@releaseFrom");
        if (releaseTo is not null) predicates.Add("w.releasedate<=@releaseTo");

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await BeginReadOnlyAsync(connection, cancellationToken);
        var rows = new List<Wp510WaitRow>(page.PageSize + 1);
        await using (var command = new NpgsqlCommand($"""
            SELECT w.waitno,w.lotno,w.productno,w.lotserial,w.status::integer,w.opno,w.customerno,
                   w.waitdescription,w.ruleno,w.createqty,w.creator,w.createdate,
                   w.releaseqty,w.releaser,w.releasedate
            FROM "YMES".tblwipwaitbasis AS w
            WHERE {string.Join("\n              AND ", predicates)}
            ORDER BY w.waitno COLLATE "C"
            LIMIT @take OFFSET @offset;
            """, connection, transaction))
        {
            AddText(command, "lotNo", lotNo);
            AddText(command, "waitNo", waitNo);
            AddText(command, "opNo", opNo);
            AddText(command, "customerNo", customerNo);
            AddTimestamp(command, "createFrom", createFrom);
            AddTimestamp(command, "createTo", createTo);
            AddTimestamp(command, "releaseFrom", releaseFrom);
            AddTimestamp(command, "releaseTo", releaseTo);
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new(
                    reader.GetString(0), reader.GetString(1), NullableString(reader, 2), reader.GetString(3),
                    reader.GetInt32(4), reader.GetString(5), NullableString(reader, 6), NullableString(reader, 7),
                    NullableString(reader, 8), reader.GetDecimal(9), reader.GetString(10), WallTime(reader, 11),
                    NullableDecimal(reader, 12), NullableString(reader, 13), NullableWallTime(reader, 14)));
            }
        }

        var result = FinishPage(page, rows);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<Wp510Page<string>> GetLookupAsync(
        string field, Wp510LookupQuery query, CancellationToken cancellationToken)
    {
        var keyword = RequiredText(query.Keyword, 100, "Keyword").ToUpperInvariant();
        var column = field.Trim().ToLowerInvariant() switch
        {
            "opno" => "w.opno",
            "customerno" => "w.customerno",
            _ => throw Bad(400, "Lookup field 僅支援 opNo、customerNo。")
        };
        var page = NormalizePage(query.Page, query.PageSize);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await BeginReadOnlyAsync(connection, cancellationToken);
        var values = new List<string>(page.PageSize + 1);
        await using (var command = new NpgsqlCommand($"""
            SELECT lookup.value
            FROM (
                SELECT DISTINCT {column} AS value
                FROM "YMES".tblwipwaitbasis AS w
                WHERE w.waitsource=0 AND {column} IS NOT NULL
                  AND strpos(upper({column}),@keyword)>0
            ) AS lookup
            ORDER BY lookup.value COLLATE "C"
            LIMIT @take OFFSET @offset;
            """, connection, transaction))
        {
            command.Parameters.Add("keyword", NpgsqlDbType.Varchar).Value = keyword;
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) values.Add(reader.GetString(0));
        }

        var result = FinishPage(page, values);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<Wp510Page<Wp510DispositionRow>> GetDispositionsAsync(
        string waitNoValue, int? requestedPage, int? requestedPageSize, CancellationToken cancellationToken)
    {
        var waitNo = RequiredText(waitNoValue, 20, "WaitNo").ToUpperInvariant();
        var page = NormalizePage(requestedPage, requestedPageSize, MaxDispositionPageSize);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await BeginReadOnlyAsync(connection, cancellationToken);

        await using (var parent = new NpgsqlCommand("""
            SELECT 1 FROM "YMES".tblwipwaitbasis
            WHERE waitno=@waitNo AND waitsource=0;
            """, connection, transaction))
        {
            parent.Parameters.Add("waitNo", NpgsqlDbType.Varchar).Value = waitNo;
            if (await parent.ExecuteScalarAsync(cancellationToken) is null)
                throw Bad(404, "找不到指定的 WIP 等待單。");
        }

        var rows = new List<Wp510DispositionRow>(page.PageSize + 1);
        await using (var command = new NpgsqlCommand("""
            SELECT d.waitno,d.lotdisptype,d.description,d.creator,d.createdate,
                   n.nodeno,n.processno,n.processversion
            FROM "YMES".tblwipwaitlotdisposition AS d
            LEFT JOIN "YMES".tblprsnodebasis AS n ON n.nodeid=d.nodeid
            WHERE d.waitno=@waitNo
            ORDER BY d.createdate DESC, d.lotserial COLLATE "C", d.lotdisptype, COALESCE(d.nodeid,'') COLLATE "C"
            LIMIT @take OFFSET @offset;
            """, connection, transaction))
        {
            command.Parameters.Add("waitNo", NpgsqlDbType.Varchar).Value = waitNo;
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new(waitNo, checked((int)reader.GetDecimal(1)), NullableString(reader, 2), reader.GetString(3),
                    WallTime(reader, 4), NullableString(reader, 5), NullableString(reader, 6), NullableString(reader, 7)));
            }
        }

        var result = FinishPage(page, rows);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static async Task<NpgsqlTransaction> BeginReadOnlyAsync(
        NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await using var command = new NpgsqlCommand("""
            SET TRANSACTION READ ONLY;
            SET LOCAL lock_timeout = '2s';
            SET LOCAL statement_timeout = '10s';
            """, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return transaction;
    }

    private static DateTime? ParseWallTime(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var formats = new[] { "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm" };
        if (!DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
            throw Bad(400, $"{field} 必須是台灣本地時間，格式為 YYYY-MM-DDTHH:mm[:ss]。" );
        return DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
    }

    private static void ValidateRange(DateTime? from, DateTime? to, string label)
    {
        if (from.HasValue != to.HasValue) throw Bad(400, $"{label}需同時提供起訖時間。");
        if (from.HasValue && to.HasValue && from.Value > to.Value) throw Bad(400, $"{label}起始時間不可晚於結束時間。");
    }

    private static string? OptionalText(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        try { _ = new UTF8Encoding(false, true).GetByteCount(normalized); }
        catch (EncoderFallbackException) { throw Bad(400, $"{field} 含無效 Unicode。"); }
        if (normalized.Contains('\0') || normalized.Any(char.IsControl) || normalized.EnumerateRunes().Count() > maxLength)
            throw Bad(400, $"{field} 長度或字元不正確（最多 {maxLength} 字元）。");
        return normalized;
    }

    private static string RequiredText(string? value, int maxLength, string field) =>
        OptionalText(value, maxLength, field) ?? throw Bad(400, $"{field} 為必填欄位。");

    private static void AddText(NpgsqlCommand command, string name, string? value)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Varchar);
        parameter.Value = (object?)value ?? DBNull.Value;
    }

    private static void AddTimestamp(NpgsqlCommand command, string name, DateTime? value)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Timestamp);
        parameter.Value = (object?)value ?? DBNull.Value;
    }

    private static string? NullableString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static decimal? NullableDecimal(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);

    private static DateTime WallTime(NpgsqlDataReader reader, int ordinal) =>
        DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Unspecified);

    private static DateTime? NullableWallTime(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : WallTime(reader, ordinal);

    private static NormalizedPage NormalizePage(int? requestedPage, int? requestedPageSize, int maxPageSize = MaxPageSize)
    {
        var page = requestedPage ?? 1;
        var pageSize = requestedPageSize ?? 50;
        if (page is < 1 or > MaxPage) throw Bad(400, $"頁碼必須介於 1 到 {MaxPage:N0}。");
        if (pageSize is < 1 || pageSize > maxPageSize) throw Bad(400, $"每頁筆數必須介於 1 到 {maxPageSize}。");
        return new(page, pageSize, checked((page - 1) * pageSize));
    }

    private static Wp510Page<T> FinishPage<T>(NormalizedPage page, List<T> rows)
    {
        var hasMore = rows.Count > page.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        return new(page.Page, page.PageSize, hasMore, rows);
    }

    private static Wp510RuleException Bad(int status, string message) => new(status, message);
    private sealed record NormalizedPage(int Page, int PageSize, int Offset);
}
