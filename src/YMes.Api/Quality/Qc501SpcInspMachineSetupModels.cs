namespace YMes.Api.Quality;

public sealed record Qc501SpcSetupVariableItemDto(
    string QcItemNo,
    string QcItemName,
    int CalType);

public sealed record Qc501SpcSetupGroupItemDto(
    string GroupNo,
    string? GroupName,
    string QcItemNo,
    string? QcItemName,
    int ShowOrder);

public sealed record Qc501SpcSetupInspectionMachineDto(
    string InspMachineNo,
    string? InspMachineName,
    int InspType,
    decimal? BaudRate,
    string? ParityBit,
    int? DataBit,
    int? StopBit,
    string? EndCode,
    string? Format1,
    string? Format2,
    string? ChNo);

public sealed record Qc501SpcSetupLookupsDto(
    IReadOnlyList<Qc501SpcSetupVariableItemDto> VariableItems,
    IReadOnlyList<Qc501SpcSetupGroupItemDto> GroupItems,
    IReadOnlyList<Qc501SpcSetupInspectionMachineDto> InspectionMachines);
