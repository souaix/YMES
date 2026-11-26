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

public sealed class EquipmentMttrSummaryRuleException(int statusCode, string message)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
