namespace YMes.Api.Reports;

public sealed record EquipmentMttrSummaryRow(
    string EquipmentNo,
    string EquipmentType,
    string? AreaNo,
    string? BelongEquipmentNo,
    bool IsChamber,
    long BreakdownTimes,
    decimal MttrHours,
    decimal MtbfHours,
    decimal AvailabilityRatio);

public sealed record EquipmentMttrSummaryResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    bool IncludeChambers,
    IReadOnlyList<EquipmentMttrSummaryRow> Items);

public sealed record EquipmentMttrMonthlyValue(
    string YearMonth,
    long BreakdownTimes,
    decimal? MttrHours,
    decimal? MtbfHours,
    decimal? AvailabilityRatio);

public sealed record EquipmentMttrMonthlyItem(
    string EquipmentNo,
    string EquipmentType,
    string? AreaNo,
    string? MainEquipmentNo,
    bool IsChamber,
    IReadOnlyList<EquipmentMttrMonthlyValue> Months);

public sealed record EquipmentMttrMonthlyResponse(
    string StartMonth,
    string EndMonth,
    string Scope,
    IReadOnlyList<EquipmentMttrMonthlyItem> Items);

public sealed class EquipmentMttrSummaryRuleException(int statusCode, string message)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
