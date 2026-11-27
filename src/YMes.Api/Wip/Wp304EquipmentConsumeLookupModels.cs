namespace YMes.Api.Wip;

public sealed class Wp304EquipmentConsumeLookupQuery
{
    public string? Keyword { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record Wp304EquipmentLookupItem(
    string EquipmentNo,
    string EquipmentType,
    string? Description);

public sealed record Wp304ConsumableBasisLookupItem(
    string EquipmentType,
    string ConsumeNo);
