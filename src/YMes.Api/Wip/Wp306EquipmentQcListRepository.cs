using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed class Wp306EquipmentQcListRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 10_000;
    private const int MaxPageSize = 100;
    private static readonly TimeZoneInfo TaipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    public async Task<Wp306Page<Wp306EquipmentQcListHeader>> GetHeadersAsync(
        Wp306EquipmentQcListQuery query, CancellationToken cancellationToken)
    {
        var equipmentNo = OptionalText(query.EquipmentNo, 50, "EquipmentNo")?.ToUpperInvariant();
        var start = ToTaipeiWallTime(query.StartDate);
        var end = ToTaipeiWallTime(query.EndDate);
        if (equipmentNo is null && (!start.HasValue || !end.HasValue))
            throw Bad("請指定 EquipmentNo，或同時指定 StartDate 與 EndDate；不提供無條件全表查詢。" );
        if (start.HasValue && end.HasValue && start.Value > end.Value)
            throw Bad("StartDate 不可晚於 EndDate。" );
        var page = NormalizePage(query.Page, query.PageSize);
        var filters = new List<string>();
        if (equipmentNo is not null) filters.Add("equipmentno = @equipmentNo");
        if (start.HasValue) filters.Add("createdate >= @startDate");
        if (end.HasValue) filters.Add("createdate <= @endDate");

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await BeginReadOnlyAsync(connection, cancellationToken);
        var rows = new List<Wp306EquipmentQcListHeader>(page.PageSize + 1);
        var sql = $"""
            SELECT qclistserial, equipmentno, qclistno, lotserial, loggroupserial,
                   qcresult, description, creator, createdate, reviser, revisedate,
                   qctaskno, insptype, shiftno, lotno, inspstarttime, qctaskstatus,
                   belongdate, planqctime
            FROM "YMES".tblwipeqpqclistlog
            WHERE {string.Join("\n              AND ", filters)}
            ORDER BY qclistserial COLLATE "C" ASC
            LIMIT @take OFFSET @offset;
            """;
        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            if (equipmentNo is not null) command.Parameters.Add("equipmentNo", NpgsqlDbType.Varchar).Value = equipmentNo;
            if (start.HasValue) command.Parameters.Add("startDate", NpgsqlDbType.Timestamp).Value = start.Value;
            if (end.HasValue) command.Parameters.Add("endDate", NpgsqlDbType.Timestamp).Value = end.Value;
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2),
                    NullableString(reader, 3), NullableString(reader, 4), reader.GetDecimal(5),
                    NullableString(reader, 6), NullableString(reader, 7), NullableDateTime(reader, 8),
                    NullableString(reader, 9), NullableDateTime(reader, 10), NullableString(reader, 11),
                    NullableDecimal(reader, 12), NullableString(reader, 13), NullableString(reader, 14),
                    NullableString(reader, 15), NullableDecimal(reader, 16), NullableDateTime(reader, 17),
                    NullableDateTime(reader, 18)));
            }
        }

        var result = FinishPage(page, rows);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<Wp306EquipmentQcListDetail>> GetDetailsAsync(
        string qcListSerial, CancellationToken cancellationToken)
    {
        var serial = RequiredText(qcListSerial, 100, "QCListSerial");
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await BeginReadOnlyAsync(connection, cancellationToken);

        await using (var parent = new NpgsqlCommand("""
            SELECT 1
            FROM "YMES".tblwipeqpqclistlog
            WHERE qclistserial = @serial;
            """, connection, transaction))
        {
            parent.Parameters.Add("serial", NpgsqlDbType.Varchar).Value = serial;
            if (await parent.ExecuteScalarAsync(cancellationToken) is null)
                throw new Wp306EquipmentQcListRuleException(404, "找不到指定的設備點檢單。" );
        }

        var rows = new List<Wp306EquipmentQcListDetail>();
        await using (var command = new NpgsqlCommand("""
            SELECT qclistserial, qcitem, qcorder, inputdatacount, qctype, inputvalue,
                   d01, d02, d03, d04, d05, d06, d07, d08, d09, d10,
                   stdvalue, minivalue, maxivalue, qcresult
            FROM "YMES".tblwipeqpqclistdetail
            WHERE qclistserial = @serial
            ORDER BY qcorder ASC, qcitem COLLATE "C" ASC;
            """, connection, transaction))
        {
            command.Parameters.Add("serial", NpgsqlDbType.Varchar).Value = serial;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new(
                    reader.GetString(0), reader.GetString(1), reader.GetDecimal(2), reader.GetDecimal(3),
                    NullableDecimal(reader, 4), NullableString(reader, 5),
                    NullableString(reader, 6), NullableString(reader, 7), NullableString(reader, 8),
                    NullableString(reader, 9), NullableString(reader, 10), NullableString(reader, 11),
                    NullableString(reader, 12), NullableString(reader, 13), NullableString(reader, 14),
                    NullableString(reader, 15), NullableString(reader, 16), NullableString(reader, 17),
                    NullableString(reader, 18), reader.GetDecimal(19)));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return rows;
    }

    private static async Task<NpgsqlTransaction> BeginReadOnlyAsync(
        NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await using var command = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return transaction;
    }

    private static string? NullableString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static decimal? NullableDecimal(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);

    private static DateTime? NullableDateTime(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Unspecified);

    private static DateTime? ToTaipeiWallTime(DateTimeOffset? value) => value is null
        ? null
        : DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(value.Value, TaipeiTimeZone).DateTime, DateTimeKind.Unspecified);

    private static NormalizedPage NormalizePage(int? page, int? pageSize)
    {
        var normalizedPage = page ?? 1;
        var normalizedPageSize = pageSize ?? 100;
        if (normalizedPage is < 1 or > MaxPage) throw Bad($"頁碼必須介於 1 到 {MaxPage:N0}。" );
        if (normalizedPageSize is < 1 or > MaxPageSize) throw Bad($"每頁筆數必須介於 1 到 {MaxPageSize}。" );
        return new(normalizedPage, normalizedPageSize, checked((normalizedPage - 1) * normalizedPageSize));
    }

    private static Wp306Page<T> FinishPage<T>(NormalizedPage page, List<T> rows)
    {
        var hasMore = rows.Count > page.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        return new(page.Page, page.PageSize, hasMore, rows);
    }

    private static string RequiredText(string? value, int maxLength, string field) =>
        OptionalText(value, maxLength, field) ?? throw Bad($"{field} 為必填欄位。" );

    private static string? OptionalText(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        try { _ = new UTF8Encoding(false, true).GetByteCount(normalized); }
        catch (EncoderFallbackException) { throw Bad($"{field} 含無效 Unicode。" ); }
        if (normalized.Contains('\0') || normalized.Any(char.IsControl) || normalized.EnumerateRunes().Count() > maxLength)
            throw Bad($"{field} 長度或字元不正確（最多 {maxLength} 字元）。" );
        return normalized;
    }

    private static Wp306EquipmentQcListRuleException Bad(string message) => new(400, message);
    private sealed record NormalizedPage(int Page, int PageSize, int Offset);
}

public sealed class Wp306EquipmentQcListRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
