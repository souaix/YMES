using MES.Contracts;

namespace MES.Web.Models;

public sealed class EquipmentIndexViewModel
{
    public IReadOnlyList<EquipmentDto> Equipment { get; init; } = [];
    public IReadOnlyList<EquipmentDto> AllEquipment { get; init; } = [];
    public IReadOnlyList<EquipmentDto> Chambers { get; init; } = [];
    public EquipmentDto? Selected { get; init; }
    public string? Keyword { get; init; }
    public int? IssueState { get; init; }
}
