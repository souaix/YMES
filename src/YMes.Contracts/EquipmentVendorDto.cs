using System.ComponentModel.DataAnnotations;

namespace MES.Contracts;

public sealed record EquipmentVendorDto
{
    public required string VendorNo { get; init; }
    public required string VendorName { get; init; }
    public string? Description { get; init; }
    public int IssueState { get; init; }
    public string? ErpNo { get; init; }
    public long DataStamp { get; init; }
    public string? Creator { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? Reviser { get; init; }
    public DateTime? RevisedAt { get; init; }
}

public sealed class EquipmentVendorWriteRequest
{
    [Required, StringLength(20)] public string VendorNo { get; set; } = "";
    [Required, StringLength(220)] public string VendorName { get; set; } = "";
    [StringLength(255)] public string? Description { get; set; }
    [StringLength(50)] public string? ErpNo { get; set; }
    public long DataStamp { get; set; }
}

public sealed record EquipmentVendorContactorDto
{
    public required string VendorNo { get; init; }
    public required string ContactorName { get; init; }
    public string? TelNo { get; init; }
    public string? FaxNo { get; init; }
    public string? Title { get; init; }
    public string? Address { get; init; }
    public string? Email { get; init; }
    public string? Description { get; init; }
    public long DataStamp { get; init; }
    public string? Creator { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? Reviser { get; init; }
    public DateTime? RevisedAt { get; init; }
}

public sealed class EquipmentVendorContactorWriteRequest
{
    [Required, StringLength(20)] public string VendorNo { get; set; } = "";
    [Required, StringLength(50)] public string ContactorName { get; set; } = "";
    [StringLength(20)] public string? TelNo { get; set; }
    [StringLength(20)] public string? FaxNo { get; set; }
    [StringLength(20)] public string? Title { get; set; }
    [StringLength(50)] public string? Address { get; set; }
    [StringLength(50), EmailAddress] public string? Email { get; set; }
    [StringLength(255)] public string? Description { get; set; }
    public long DataStamp { get; set; }
}
