using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
    public string? SapPlnal { get; init; }
    public int? IssueState { get; init; }
    public bool CurrentVersion { get; init; }
    public decimal? DataStamp { get; init; }
    public string? Creator { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? Reviser { get; init; }
    public DateTime? RevisedAt { get; init; }
}

public class ProductWriteRequest
{
    [Required, StringLength(50)] public string ProductNo { get; set; } = "";
    [Required, StringLength(5)] public string ProductVersion { get; set; } = "01";
    [Required, StringLength(200)] public string ProductName { get; set; } = "";
    // The server validates new/changed choices; unchanged legacy empty values
    // remain editable without turning them into a new selectable type.
    [StringLength(50)] public string ProductType { get; set; } = "";
    [StringLength(40)] public string UnitType { get; set; } = "";
    [Required, StringLength(40)] public string UnitNo { get; set; } = "";
    [StringLength(1000)] public string Description { get; set; } = "";
    [StringLength(40)] public string SpecNo { get; set; } = "";
    [StringLength(40)] public string ProductCode { get; set; } = "";
    [StringLength(40)] public string SerialTypeNoLot { get; set; } = "";
    [StringLength(40)] public string SerialTypeNoComp { get; set; } = "";
    [Range(0, 999999999)] public decimal LotStdQty { get; set; }
    [StringLength(40)] public string ErpNo { get; set; } = "";
    // Omitted/null preserves the existing value on update; creation stays NULL.
    [StringLength(2)] public string? SapPlnal { get; set; }
    public virtual int? IssueState { get; set; }
    public virtual decimal? DataStamp { get; set; }
}

// Creation has no expected version. Updates must distinguish an omitted token
// from an explicitly supplied NULL source value.
public sealed class ProductUpdateRequest : ProductWriteRequest
{
    [JsonRequired] public override int? IssueState { get; set; }
    [JsonRequired] public override decimal? DataStamp { get; set; }
}

public sealed class ProductCommandRequest
{
    [JsonRequired] public int? IssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
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
