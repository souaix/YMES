using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Ems;

public sealed class AccessoryStateRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 100_000;
    private const int MaxPageSize = 100;

    public AccessoryStatePage List(string? accessoryType, string? accessoryNo, int page, int pageSize)
    {
        if (page is < 1 or > MaxPage) throw Rule(400, $"頁碼需介於 1 到 {MaxPage}。 ");
        if (pageSize is < 1 or > MaxPageSize) throw Rule(400, $"每頁筆數需介於 1 到 {MaxPageSize}。 ");
        var type = Filter(accessoryType, "配件類型");
        var no = Filter(accessoryNo, "配件編號");
        var offset = checked((page - 1) * pageSize);

        using var command = dataSource.CreateCommand("""
            SELECT s.accserialno,s.accessoryno,s.accessoryversion,s.accessorytype,
                   s.accessorystate::int,b.statename,s.starttime,s.userno,s.description,s.applytime,
                   s.accumulateqty,s.accspareqty,s.accrepairqty,s.acctotalusedqty,s.timecyclestart,
                   l.repaircycle,
                   CASE WHEN l.repaircycle=-1 THEN -1::numeric
                        WHEN l.repaircycle IS NULL THEN NULL
                        ELSE l.repaircycle-s.accumulateqty END AS accspareqtylimit
            FROM "YMES".tblemsaccessorystate s
            JOIN "YMES".tbleqpaccstatebasis b ON b.accessorystate=s.accessorystate
            LEFT JOIN "YMES".tbleqpaccessorylife l ON l.accessory=s.accessorytype
            WHERE (@accessoryType IS NULL OR s.accessorytype=@accessoryType)
              AND (@accessoryNo IS NULL OR s.accessoryno=@accessoryNo)
            ORDER BY s.accessorytype COLLATE "C",s.accessoryno COLLATE "C",
                     s.starttime,s.accessoryversion COLLATE "C"
            LIMIT @take OFFSET @skip;
            """);
        command.Parameters.Add("accessoryType", NpgsqlDbType.Varchar).Value = (object?)type ?? DBNull.Value;
        command.Parameters.Add("accessoryNo", NpgsqlDbType.Varchar).Value = (object?)no ?? DBNull.Value;
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = pageSize + 1;
        command.Parameters.Add("skip", NpgsqlDbType.Integer).Value = offset;

        using var reader = command.ExecuteReader();
        var items = new List<AccessoryStateRow>(pageSize + 1);
        while (reader.Read()) items.Add(Map(reader));
        var hasMore = items.Count > pageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);
        return new AccessoryStatePage(page, pageSize, hasMore, items);
    }

    private static AccessoryStateRow Map(NpgsqlDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
        reader.GetInt32(4), reader.GetString(5), reader.GetDateTime(6), reader.GetString(7),
        NullableString(reader, 8), NullableDate(reader, 9), NullableDecimal(reader, 10),
        NullableDecimal(reader, 11), NullableDecimal(reader, 12), NullableDecimal(reader, 13),
        NullableDate(reader, 14), NullableDecimal(reader, 15), NullableDecimal(reader, 16));

    private static string? Filter(string? value, string name)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > 50) throw Rule(400, $"{name}不可超過 50 個字元。");
        return normalized;
    }

    private static string? NullableString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static DateTime? NullableDate(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);

    private static decimal? NullableDecimal(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);

    private static AccessoryStateRuleException Rule(int status, string message) => new(status, message.Trim());
}

public sealed record AccessoryStatePage(
    int Page,
    int PageSize,
    bool HasMore,
    IReadOnlyList<AccessoryStateRow> Items);

public sealed record AccessoryStateRow(
    string AccSerialNo,
    string AccessoryNo,
    string AccessoryVersion,
    string AccessoryType,
    int AccessoryState,
    string StateName,
    DateTime StartTime,
    string UserNo,
    string? Description,
    DateTime? ApplyTime,
    decimal? AccumulateQty,
    decimal? AccSpareQty,
    decimal? AccRepairQty,
    decimal? AccTotalUsedQty,
    DateTime? TimeCycleStart,
    decimal? RepairCycle,
    decimal? AccSpareQtyLimit);

public sealed class AccessoryStateRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
