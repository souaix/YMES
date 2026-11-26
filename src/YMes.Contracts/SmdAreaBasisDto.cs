using System.ComponentModel.DataAnnotations;

namespace MES.Contracts;

public sealed record SmdAreaBasisDto
{
    public required string AreaNo { get; init; }
    public required string AreaName { get; init; }
    public required int AreaType { get; init; }
    public required int AreaClass { get; init; }
    public string? BaseAreaNo { get; init; }
    public required string FactoryNo { get; init; }
    public string? DepartmentNo { get; init; }
    public string? Description { get; init; }
    public required string Creator { get; init; }
    public DateTime CreateDate { get; init; }
    public required int IssueState { get; init; }
    public required int AreaCompose { get; init; }
    public string? Reviser { get; init; }
    public DateTime? ReviseDate { get; init; }
    public long? DataStamp { get; init; }
}

public sealed class SmdAreaBasisWriteRequest
{
    [Required, StringLength(20)] public string AreaNo { get; set; } = "";
    [Required, StringLength(50)] public string AreaName { get; set; } = "";
    [Required, Range(0, 2)] public int? AreaType { get; set; }
    [Required, Range(0, 1)] public int? AreaClass { get; set; }
    [StringLength(20)] public string? BaseAreaNo { get; set; }
    [Required, StringLength(20)] public string FactoryNo { get; set; } = "";
    [Required, StringLength(20)] public string DepartmentNo { get; set; } = "";
    [StringLength(255)] public string? Description { get; set; }
    [Required, Range(0, 1)] public int? AreaCompose { get; set; }
}
