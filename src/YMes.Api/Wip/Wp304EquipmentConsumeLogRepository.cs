using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed class Wp304EquipmentConsumeLogRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 10_000;
    private const int MaxPageSize = 100;
    private static readonly TimeZoneInfo TaipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    public async Task<Wp304EquipmentConsumeLogPage<Wp304EquipmentConsumeLogItem>> GetAsync(
        Wp304EquipmentConsumeLogQuery query, CancellationToken cancellationToken)
    {
        var equipmentNo = OptionalText(query.EquipmentNo, 50, "EquipmentNo")?.ToUpperInvariant();
        var consumeNo = OptionalText(query.ConsumeNo, 25, "ConsumeNo");
        var start = ToTaipeiWallTime(query.StartTime);
        var end = ToTaipeiWallTime(query.EndTime);

        if (equipmentNo is null && consumeNo is null && (!start.HasValue || !end.HasValue))
            throw Bad("請指定 EquipmentNo、ConsumeNo，或同時指定 StartTime 與 EndTime；不提供無條件全表查詢。");
        if (start.HasValue && end.HasValue && start.Value > end.Value)
            throw Bad("StartTime 不可晚於 EndTime。");

        var page = NormalizePage(query.Page, query.PageSize);
        var filters = new List<string>();
        if (equipmentNo is not null) filters.Add("equipmentno = @equipmentNo");
        if (consumeNo is not null) filters.Add("consumeno = @consumeNo");
        if (start.HasValue) filters.Add("createdate >= @startTime");
        if (end.HasValue) filters.Add("createdate <= @endTime");

        var sql = $"""
            SELECT equipmentno, consumeno, consumeseq, accqty, accperiod, creator, createdate
            FROM "YMES".tblwipeqpconsumelog
            WHERE {string.Join("\n              AND ", filters)}
            ORDER BY createdate DESC NULLS LAST,
                     equipmentno COLLATE "C",
                     consumeno COLLATE "C",
                     consumeseq COLLATE "C"
            LIMIT @take OFFSET @offset;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction))
            await readOnly.ExecuteNonQueryAsync(cancellationToken);

        var rows = new List<Wp304EquipmentConsumeLogItem>(page.PageSize + 1);
        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            if (equipmentNo is not null) command.Parameters.Add("equipmentNo", NpgsqlDbType.Varchar).Value = equipmentNo;
            if (consumeNo is not null) command.Parameters.Add("consumeNo", NpgsqlDbType.Varchar).Value = consumeNo;
            if (start.HasValue) command.Parameters.Add("startTime", NpgsqlDbType.Timestamp).Value = start.Value;
            if (end.HasValue) command.Parameters.Add("endTime", NpgsqlDbType.Timestamp).Value = end.Value;
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2),
                    NullableDecimal(reader, 3), NullableDecimal(reader, 4),
                    NullableString(reader, 5), NullableDateTime(reader, 6)));
            }
        }

        var result = FinishPage(page, rows);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static DateTime? ToTaipeiWallTime(DateTimeOffset? value) => value is null
        ? null
        : DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(value.Value, TaipeiTimeZone).DateTime, DateTimeKind.Unspecified);

    private static string? NullableString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static decimal? NullableDecimal(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);

    private static DateTime? NullableDateTime(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Unspecified);

    private static NormalizedPage NormalizePage(int? page, int? pageSize)
    {
        var normalizedPage = page ?? 1;
        var normalizedPageSize = pageSize ?? 50;
        if (normalizedPage is < 1 or > MaxPage) throw Bad($"頁碼必須介於 1 到 {MaxPage:N0}。");
        if (normalizedPageSize is < 1 or > MaxPageSize) throw Bad($"每頁筆數必須介於 1 到 {MaxPageSize}。");
        return new(normalizedPage, normalizedPageSize, checked((normalizedPage - 1) * normalizedPageSize));
    }

    private static Wp304EquipmentConsumeLogPage<T> FinishPage<T>(NormalizedPage page, List<T> rows)
    {
        var hasMore = rows.Count > page.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        return new(page.Page, page.PageSize, hasMore, rows);
    }

    private static string? OptionalText(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        try { _ = new UTF8Encoding(false, true).GetByteCount(normalized); }
        catch (EncoderFallbackException) { throw Bad($"{field} 含無效 Unicode。"); }
        if (normalized.Contains('\0') || normalized.Any(char.IsControl) || normalized.EnumerateRunes().Count() > maxLength)
            throw Bad($"{field} 長度或字元不正確（最多 {maxLength} 字元）。");
        return normalized;
    }

    private static Wp304EquipmentConsumeLogRuleException Bad(string message) => new(400, message);
    private sealed record NormalizedPage(int Page, int PageSize, int Offset);
}

public sealed class Wp304EquipmentConsumeLogRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
