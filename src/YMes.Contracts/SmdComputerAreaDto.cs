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

public sealed record SmdAreaOptionDto(
    string AreaNo,
    string AreaName,
    string? Description,
    int IssueState,
    int AreaClass,
    string FactoryNo);

public sealed record SmdAreaBasisOptionsDto(IReadOnlyList<SmdFactoryOptionDto> Factories);

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
