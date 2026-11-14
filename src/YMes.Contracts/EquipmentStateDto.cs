namespace MES.Contracts;

public sealed record ValueOptionDto(int Value, string Label);

public sealed record EquipmentStateOptionsDto(
    IReadOnlyList<ValueOptionDto> StateTypes,
    IReadOnlyList<ValueOptionDto> UtilizationTypes);

public sealed record EquipmentStateDto
{
    public required int EquipmentState { get; init; }
    public required int StateType { get; init; }
    public required string StateName { get; init; }
    public required int StateColor { get; init; }
    public string? Description { get; init; }
    public string? EngineerGroupNo { get; init; }
    public required int UtilizationType { get; init; }
    public string? StateColorHex { get; init; }
    public required string StateTypeText { get; init; }
    public required string UtilizationTypeText { get; init; }
}

public sealed record EquipmentStateWriteRequest(
    int? EquipmentState,
    int? StateType,
    string? StateName,
    int? StateColor,
    string? Description,
    int? UtilizationType,
    string? StateColorHex = null);
