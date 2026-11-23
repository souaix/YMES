using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Ems;

public sealed class EquipmentAccessoryStateLogRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 10_000;
    private const int MaxPageSize = 100;
    private const int MaxRangeDays = 366;
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd'T'HH:mm",
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.FFFFFFF"
    ];

    public async Task<EquipmentAccessoryStateLogPage> SearchAsync(
        EquipmentAccessoryStateLogFilter filter,
        CancellationToken cancellationToken)
    {
        if (filter.Page is < 1 or > MaxPage)
            throw Rule($"頁碼需介於 1 到 {MaxPage}。");
        if (filter.PageSize is < 1 or > MaxPageSize)
            throw Rule($"每頁筆數需介於 1 到 {MaxPageSize}。");

        var equipmentType = Normalize(filter.EquipmentType, "設備類別", 50);
        var equipmentNo = Normalize(filter.EquipmentNo, "設備編號", 50);
        var userNo = Normalize(filter.UserNo, "使用者編號", 10);
        var accessoryType = Normalize(filter.AccessoryType, "配件類別", 50);
        var accessoryNo = Normalize(filter.AccessoryNo, "配件編號", 50);
        var startTimeFrom = ParseDate(filter.StartTimeFrom, "Start Time From");
        var startTimeTo = ParseDate(filter.StartTimeTo, "Start Time To");
        var endTimeFrom = ParseDate(filter.EndTimeFrom, "End Time From");
        var endTimeTo = ParseDate(filter.EndTimeTo, "End Time To");

        if (startTimeFrom is null && startTimeTo is null && endTimeFrom is null && endTimeTo is null)
            throw Rule("請至少設定一個 Start Time 或 End Time 界限。");
        ValidateRange(startTimeFrom, startTimeTo, "Start Time");
        ValidateRange(endTimeFrom, endTimeTo, "End Time");

        var offset = checked((filter.Page - 1) * filter.PageSize);
        await using var command = dataSource.CreateCommand("""
            SELECT accserialno, equipmenttype, equipmentno, accessorytype, accessoryno,
                   accessoryversion, userno, starttime, endtime
            FROM "YMES".tblemseqpaccstatelog
            WHERE (@equipmentType IS NULL OR equipmenttype = @equipmentType)
              AND (@equipmentNo IS NULL OR equipmentno = @equipmentNo)
              AND (@userNo IS NULL OR userno = @userNo)
              AND (@accessoryType IS NULL OR accessorytype = @accessoryType)
              AND (@accessoryNo IS NULL OR accessoryno = @accessoryNo)
              AND (@startTimeFrom IS NULL OR starttime >= @startTimeFrom)
              AND (@startTimeTo IS NULL OR starttime <= @startTimeTo)
              AND (@endTimeFrom IS NULL OR endtime >= @endTimeFrom)
              AND (@endTimeTo IS NULL OR endtime <= @endTimeTo)
            ORDER BY starttime ASC NULLS LAST,
                     equipmenttype COLLATE "C", equipmentno COLLATE "C",
                     accessorytype COLLATE "C", accessoryno COLLATE "C",
                     accserialno COLLATE "C"
            LIMIT @take OFFSET @skip;
            """);
        AddText(command, "equipmentType", equipmentType);
        AddText(command, "equipmentNo", equipmentNo);
        AddText(command, "userNo", userNo);
        AddText(command, "accessoryType", accessoryType);
        AddText(command, "accessoryNo", accessoryNo);
        AddDate(command, "startTimeFrom", startTimeFrom);
        AddDate(command, "startTimeTo", startTimeTo);
        AddDate(command, "endTimeFrom", endTimeFrom);
        AddDate(command, "endTimeTo", endTimeTo);
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = filter.PageSize + 1;
        command.Parameters.Add("skip", NpgsqlDbType.Integer).Value = offset;

        var rows = new List<EquipmentAccessoryStateLogRow>(filter.PageSize + 1);
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new EquipmentAccessoryStateLogRow(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2),
                    reader.GetString(3), reader.GetString(4), reader.GetString(5),
                    NullableString(reader, 6), NullableDate(reader, 7), NullableDate(reader, 8)));
            }
        }

        var hasMore = rows.Count > filter.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        return new EquipmentAccessoryStateLogPage(filter.Page, filter.PageSize, hasMore, rows);
    }

    private static string? Normalize(string? value, string label, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > maxLength) throw Rule($"{label}不可超過 {maxLength} 個字元。");
        return normalized.ToUpperInvariant();
    }

    private static DateTime? ParseDate(string? value, string label)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (!DateTime.TryParseExact(normalized, DateFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
            throw Rule($"{label}格式無效，請使用本地日期時間。");
        return DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
    }

    private static void ValidateRange(DateTime? from, DateTime? to, string label)
    {
        if (from is not null && to is not null && from > to)
            throw Rule($"{label}起始時間不可晚於結束時間。");
        if (from is not null && to is not null && to.Value - from.Value > TimeSpan.FromDays(MaxRangeDays))
            throw Rule($"{label}查詢區間不可超過 {MaxRangeDays} 天。");
    }

    private static void AddText(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static void AddDate(NpgsqlCommand command, string name, DateTime? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Timestamp).Value = (object?)value ?? DBNull.Value;

    private static string? NullableString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static DateTime? NullableDate(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);

    private static EquipmentAccessoryStateLogRuleException Rule(string message) =>
        new(400, message);
}

public sealed record EquipmentAccessoryStateLogFilter(
    string? EquipmentType,
    string? EquipmentNo,
    string? UserNo,
    string? AccessoryType,
    string? AccessoryNo,
    string? StartTimeFrom,
    string? StartTimeTo,
    string? EndTimeFrom,
    string? EndTimeTo,
    int Page,
    int PageSize);

public sealed record EquipmentAccessoryStateLogPage(
    int Page,
    int PageSize,
    bool HasMore,
    IReadOnlyList<EquipmentAccessoryStateLogRow> Items);

public sealed record EquipmentAccessoryStateLogRow(
    string AccSerialNo,
    string EquipmentType,
    string EquipmentNo,
    string AccessoryType,
    string AccessoryNo,
    string AccessoryVersion,
    string? UserNo,
    DateTime? StartTime,
    DateTime? EndTime);

public sealed class EquipmentAccessoryStateLogRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
