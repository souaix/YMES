namespace YMes.Api.Wip;

public sealed class Wp306EquipmentQcListQuery
{
    public string? EquipmentNo { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record Wp306EquipmentQcListHeader(
    string QCListSerial,
    string EquipmentNo,
    string QCListNo,
    string? LotSerial,
    string? LogGroupSerial,
    decimal QCResult,
    string? Description,
    string? Creator,
    DateTime? CreateDate,
    string? Reviser,
    DateTime? ReviseDate,
    string? QCTaskNo,
    decimal? InspType,
    string? ShiftNo,
    string? LotNo,
    string? InspStartTime,
    decimal? QCTaskStatus,
    DateTime? BelongDate,
    DateTime? PlanQCTime);

public sealed record Wp306EquipmentQcListDetail(
    string QCListSerial,
    string QCItem,
    decimal QCOrder,
    decimal InputDataCount,
    decimal? Type,
    string? InputValue,
    string? D01,
    string? D02,
    string? D03,
    string? D04,
    string? D05,
    string? D06,
    string? D07,
    string? D08,
    string? D09,
    string? D10,
    string? StdValue,
    string? MiniValue,
    string? MaxiValue,
    decimal QCResult);

public sealed record Wp306Page<T>(int Page, int PageSize, bool HasMore, IReadOnlyList<T> Items);
