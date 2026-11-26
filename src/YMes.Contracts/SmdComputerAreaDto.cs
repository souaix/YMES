using System.ComponentModel.DataAnnotations;

namespace MES.Contracts;

public sealed record SmdComputerAreaDto
{
    public required string ComputerNo { get; init; }
    public required string FactoryNo { get; init; }
    public string? Creator { get; init; }
    public DateTime? CreateDate { get; init; }
    public string? Reviser { get; init; }
    public DateTime? ReviseDate { get; init; }
    public long? DataStamp { get; init; }
    public required IReadOnlyList<string> Areas { get; init; }
}

public sealed record SmdFactoryOptionDto(string FactoryNo, string? Description, string? FactoryName);

public sealed record SmdFactoryBasisDto
{
    public required string FactoryNo { get; init; }
    public string? Description { get; init; }
    public required string Creator { get; init; }
    public DateTime CreateDate { get; init; }
    public required int IssueState { get; init; }
    public string? FabCode { get; init; }
    public string? FactoryName { get; init; }
    public string? Reviser { get; init; }
    public DateTime? ReviseDate { get; init; }
    public long? DataStamp { get; init; }
}

public sealed record SmdFactoryBasisOptionsDto(bool FactoryNameEnabled);

public sealed class SmdFactoryBasisWriteRequest
{
    [Required, StringLength(20)] public string FactoryNo { get; set; } = "";
    [StringLength(50)] public string? FactoryName { get; set; }
    [StringLength(10)] public string? FabCode { get; set; }
    [StringLength(255)] public string? Description { get; set; }
}

public sealed class SmdFactoryBasisExpectedRequest
{
    [Required, Range(0, 0)] public int? IssueState { get; set; }
    [Required, Range(0, long.MaxValue)] public long? DataStamp { get; set; }
}

public sealed record SmdAreaOptionDto(
    string AreaNo,
    string AreaName,
    string? Description,
    int IssueState,
    int AreaClass,
    string FactoryNo);

public sealed record SmdAreaBasisOptionsDto(
    IReadOnlyList<SmdFactoryOptionDto> Factories,
    IReadOnlyList<SmdAreaOptionDto> BaseAreas);

public sealed record SmdAreaInventoryDto
{
    public required string AreaNo { get; init; }
    public required string InventoryNo { get; init; }
    public required string BaseAreaNo { get; init; }
    public required int DefaultInv { get; init; }
    public string? Creator { get; init; }
    public DateTime? CreateDate { get; init; }
    public string? Reviser { get; init; }
    public DateTime? ReviseDate { get; init; }
    public long? DataStamp { get; init; }
}

public sealed record SmdAreaRelationOptionDto(
    string ObjectNo,
    string ObjectText,
    int ObjectType,
    string? ContainAreaNo,
    int? ObjectAreaType);

public sealed record SmdAreaLayoutRelationsDto(IReadOnlyList<SmdAreaRelationOptionDto> Relations);

public sealed class SmdAreaInventoryWriteRequest
{
    [Required, StringLength(50)] public string AreaNo { get; set; } = "";
    [Required, StringLength(50)] public string InventoryNo { get; set; } = "";
    [Required, StringLength(50)] public string BaseAreaNo { get; set; } = "";
    [Required] public int? DefaultInv { get; set; }
}

public sealed class SmdAreaInventoryUpdateRequest
{
    [Required, StringLength(50)] public string BaseAreaNo { get; set; } = "";
    [Required] public int? DefaultInv { get; set; }
}

public sealed class SmdComputerAreaWriteRequest
{
    [Required, StringLength(20)] public string ComputerNo { get; set; } = "";
    [Required, StringLength(20)] public string FactoryNo { get; set; } = "";
    [Required] public List<string> Areas { get; set; } = [];
}

public sealed class SmdComputerAreaUpdateRequest
{
    [Required] public List<string> Areas { get; set; } = [];
}

public sealed class SmdComputerAreaCopyRequest
{
    [Required, StringLength(20)] public string FromComputerNo { get; set; } = "";
    [Required, StringLength(20)] public string FromFactoryNo { get; set; } = "";
    [Required, StringLength(20)] public string ToComputerNo { get; set; } = "";
}

public sealed record SmdPdLineBasisDto
{
    public required string PdLineNo { get; init; }
    public required string PdLineName { get; init; }
    public required int PdLineStatus { get; init; }
    public required int IssueState { get; init; }
    public string? Description { get; init; }
    public string? Creator { get; init; }
    public DateTime? CreateDate { get; init; }
    public string? Reviser { get; init; }
    public DateTime? ReviseDate { get; init; }
    public long? DataStamp { get; init; }
}

public sealed record SmdPdLineBasisOptionsDto(IReadOnlyList<ValueOptionDto> PdLineStatuses);

public sealed class SmdPdLineBasisWriteRequest
{
    [Required, StringLength(50)] public string PdLineNo { get; set; } = "";
    [Required, StringLength(50)] public string PdLineName { get; set; } = "";
    [Required] public int? PdLineStatus { get; set; }
    [StringLength(255)] public string? Description { get; set; }
}
