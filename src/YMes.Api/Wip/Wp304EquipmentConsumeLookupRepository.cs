using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed class Wp304EquipmentConsumeLookupRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 10_000;
    private const int MaxPageSize = 100;

    public Task<Wp304EquipmentConsumeLogPage<Wp304EquipmentLookupItem>> EquipmentAsync(
        Wp304EquipmentConsumeLookupQuery query, CancellationToken cancellationToken) =>
        ReadAsync(
            """
            SELECT equipmentno, equipmenttype, equipmentname
            FROM "YMES".tbleqpequipmentbasis
            WHERE issuestate = 2
              AND COALESCE(chamberflag, 0) <> 1
              AND (@keyword IS NULL OR strpos(upper(equipmentno), upper(@keyword)) > 0
                   OR strpos(upper(equipmenttype), upper(@keyword)) > 0
                   OR strpos(upper(COALESCE(equipmentname, '')), upper(@keyword)) > 0)
            ORDER BY equipmenttype COLLATE "C", equipmentno COLLATE "C"
            LIMIT @take OFFSET @offset;
            """,
            query,
            cancellationToken,
            MapEquipment);

    public Task<Wp304EquipmentConsumeLogPage<Wp304ConsumableBasisLookupItem>> ConsumablesAsync(
        Wp304EquipmentConsumeLookupQuery query, CancellationToken cancellationToken) =>
        ReadAsync(
            """
            SELECT equipmenttype, consumeno
            FROM "YMES".tbleqpconsumebasis
            WHERE @keyword IS NULL OR strpos(upper(consumeno), upper(@keyword)) > 0
                              OR strpos(upper(equipmenttype), upper(@keyword)) > 0
            ORDER BY consumeno COLLATE "C", equipmenttype COLLATE "C"
            LIMIT @take OFFSET @offset;
            """,
            query,
            cancellationToken,
            MapConsumable);

    private async Task<Wp304EquipmentConsumeLogPage<T>> ReadAsync<T>(
        string sql,
        Wp304EquipmentConsumeLookupQuery query,
        CancellationToken cancellationToken,
        Func<NpgsqlDataReader, T> map)
    {
        var keyword = OptionalText(query.Keyword, 100, "Keyword");
        var page = NormalizePage(query.Page, query.PageSize);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction))
            await readOnly.ExecuteNonQueryAsync(cancellationToken);

        var rows = new List<T>(page.PageSize + 1);
        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            command.Parameters.Add("keyword", NpgsqlDbType.Varchar).Value = (object?)keyword ?? DBNull.Value;
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) rows.Add(map(reader));
        }

        var result = FinishPage(page, rows);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static Wp304EquipmentLookupItem MapEquipment(NpgsqlDataReader reader) =>
        new(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2));

    private static Wp304ConsumableBasisLookupItem MapConsumable(NpgsqlDataReader reader) =>
        new(reader.GetString(0), reader.GetString(1));

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
