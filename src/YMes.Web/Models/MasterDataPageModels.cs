using MES.Contracts;

namespace MES.Web.Models;

public sealed class OperationIndexViewModel
{
    public IReadOnlyList<OperationDto> Items { get; init; } = [];
    public OperationDto? Selected { get; init; }
    public string? Keyword { get; init; }
    public int? IssueState { get; init; }
}

public sealed class MaterialIndexViewModel
{
    public IReadOnlyList<MaterialDto> Items { get; init; } = [];
    public MaterialDto? Selected { get; init; }
    public string? Keyword { get; init; }
    public int? IssueState { get; init; }
}

public sealed class ProcessIndexViewModel
{
    public IReadOnlyList<ProcessDto> Items { get; init; } = [];
    public ProcessDto? Selected { get; init; }
    public ProcessGraphDto? Graph { get; init; }
    public string? Keyword { get; init; }
    public int? IssueState { get; init; }
}

public sealed class ProcessGraphPageModel
{
    public ProcessDto Process { get; init; } = default!;
    public ProcessGraphDto Graph { get; init; } = default!;
    public string NodesJson { get; init; } = "[]";
    public string EdgesJson { get; init; } = "[]";
}

public sealed class ProductIndexViewModel
{
    public IReadOnlyList<ProductDto> Items { get; init; } = [];
    public ProductDto? Selected { get; init; }
    public IReadOnlyList<ProductProcessDto> Processes { get; init; } = [];
    public IReadOnlyList<ProductBomDto> Bom { get; init; } = [];
    public IReadOnlyList<ProcessDto> AvailableProcesses { get; init; } = [];
    public IReadOnlyList<MaterialDto> AvailableMaterials { get; init; } = [];
    public IReadOnlyList<OperationDto> AvailableOperations { get; init; } = [];
    public string? Keyword { get; init; }
    public int? IssueState { get; init; }
}
