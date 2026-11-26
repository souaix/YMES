namespace YMes.Api.Reports;

public sealed record EquipmentDailyUtilizationState(
    int EquipmentState,
    int StateType,
    string StateName,
    int UtilizationType,
    long StateColor,
    string StateColorHex);

public sealed record EquipmentDailyUtilizationStatePercent(
    int EquipmentState,
    decimal Percent,
    decimal Seconds);

public sealed record EquipmentDailyUtilizationRow(
    string EquipmentNo,
    string EquipmentType,
    string? EquipmentName,
    string? AreaNo,
    string? BelongEquipmentNo,
    bool IsChamber,
    decimal Moves,
    decimal LotQtyEqp,
    bool HasStateData,
    decimal LossPercentage,
    IReadOnlyList<EquipmentDailyUtilizationStatePercent> StatePercentages,
    IReadOnlyList<EquipmentDailyUtilizationRow> Children);

public sealed record EquipmentDailyUtilizationResponse(
    DateTime StartTime,
    DateTime EndTime,
    bool ChamberManage,
    IReadOnlyList<EquipmentDailyUtilizationState> States,
    IReadOnlyList<EquipmentDailyUtilizationRow> Items);

public sealed class EquipmentDailyUtilizationRuleException(int statusCode, string message)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
