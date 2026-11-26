using System.ComponentModel.DataAnnotations;

namespace MES.Contracts;

public sealed record SmdLocatorBasisDto
{
    public required string InventoryNo { get; init; }
    public required string LocatorNo { get; init; }
    public string? LocatorName { get; init; }
    public decimal? Capacity { get; init; }
    public string? Description { get; init; }
    public required string Creator { get; init; }
    public DateTime CreateDate { get; init; }
    public required int IssueState { get; init; }
    public string? ErpNo { get; init; }
    public string? Reviser { get; init; }
    public DateTime? ReviseDate { get; init; }
    public long? DataStamp { get; init; }
}

public sealed record SmdLocatorInventoryOptionDto(string InventoryNo);

public sealed record SmdLocatorBasisOptionsDto(IReadOnlyList<SmdLocatorInventoryOptionDto> ApprovedInventories);

public sealed class SmdLocatorBasisWriteRequest
{
    [Required, StringLength(20)] public string InventoryNo { get; set; } = "";
    [Required, StringLength(20)] public string LocatorNo { get; set; } = "";
    [StringLength(50)] public string? LocatorName { get; set; }
    [Range(typeof(decimal), "0", "99999999.9999")] public decimal? Capacity { get; set; }
    [StringLength(255)] public string? Description { get; set; }
}
