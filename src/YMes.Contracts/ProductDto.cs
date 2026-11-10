using System.ComponentModel.DataAnnotations;

namespace MES.Contracts;

public sealed record ProductDto
{
    public required string ProductNo { get; init; }
    public required string ProductVersion { get; init; }
    public required string ProductName { get; init; }
    public required string ProductType { get; init; }
    public string UnitType { get; init; } = "";
    public required string UnitNo { get; init; }
    public string Description { get; init; } = "";
    public string SpecNo { get; init; } = "";
    public string ProductCode { get; init; } = "";
    public string SerialTypeNoLot { get; init; } = "";
    public string SerialTypeNoComp { get; init; } = "";
    public decimal LotStdQty { get; init; }
    public string ErpNo { get; init; } = "";
    public int IssueState { get; init; }
    public bool CurrentVersion { get; init; }
    public int DataStamp { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class ProductWriteRequest
{
    [Required, StringLength(40)] public string ProductNo { get; set; } = "";
    [Required, StringLength(10)] public string ProductVersion { get; set; } = "01";
    [Required, StringLength(200)] public string ProductName { get; set; } = "";
    [Required, StringLength(40)] public string ProductType { get; set; } = "";
    [StringLength(40)] public string UnitType { get; set; } = "";
    [Required, StringLength(40)] public string UnitNo { get; set; } = "";
    [StringLength(1000)] public string Description { get; set; } = "";
    [StringLength(40)] public string SpecNo { get; set; } = "";
    [StringLength(40)] public string ProductCode { get; set; } = "";
    [StringLength(40)] public string SerialTypeNoLot { get; set; } = "";
    [StringLength(40)] public string SerialTypeNoComp { get; set; } = "";
    [Range(0, 999999999)] public decimal LotStdQty { get; set; }
    [StringLength(40)] public string ErpNo { get; set; } = "";
    public int DataStamp { get; set; }
}

public sealed record ProductProcessDto(
    string ProductNo,
    string ProductVersion,
    int MoTypeNo,
    string ProductionSegmentNo,
    int SegmentOrder,
    string ProcessNo,
    string ProcessVersionSelector,
    string? ResolvedProcessVersion,
    bool DefaultProcess,
    bool HaveComponent,
    bool HaveLevel);

public sealed class ProductProcessWriteRequest
{
    public int MoTypeNo { get; set; }
    [Required, StringLength(40)] public string ProductionSegmentNo { get; set; } = "";
    [Required, StringLength(40)] public string ProcessNo { get; set; } = "";
    [Required, StringLength(10)] public string ProcessVersionSelector { get; set; } = "*";
    public bool DefaultProcess { get; set; }
}

public sealed record ProductBomDto(
    string Id,
    string ProductNo,
    string ProductVersion,
    string ComponentType,
    string ComponentNo,
    string InputOperationNo,
    decimal StandardQty,
    string PutInPlaceType,
    bool Specified,
    decimal DecreaseRate,
    string Description);

public sealed class ProductBomWriteRequest
{
    [Required] public string ComponentType { get; set; } = "MATERIAL";
    [Required, StringLength(40)] public string ComponentNo { get; set; } = "";
    [Required, StringLength(20)] public string InputOperationNo { get; set; } = "";
    [Range(0.000001, 999999999)] public decimal StandardQty { get; set; } = 1;
    [StringLength(40)] public string PutInPlaceType { get; set; } = "";
    public bool Specified { get; set; }
    [Range(0, 100)] public decimal DecreaseRate { get; set; }
    [StringLength(1000)] public string Description { get; set; } = "";
}
