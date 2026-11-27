namespace YMes.Api.Wip;

public sealed class Wp304EquipmentConsumeLogQuery
{
    public string? EquipmentNo { get; set; }
    public string? ConsumeNo { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record Wp304EquipmentConsumeLogItem(
    string EquipmentNo,
    string ConsumeNo,
    string ConsumeSeq,
    decimal? AccQty,
    decimal? AccPeriod,
    string? Creator,
    DateTime? CreateDate);

public sealed record Wp304EquipmentConsumeLogPage<T>(
    int Page,
    int PageSize,
    bool HasMore,
    IReadOnlyList<T> Items);
