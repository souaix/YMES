using System.ComponentModel.DataAnnotations;

namespace MES.Contracts;

public sealed record OperationDto
{
    public required string OperationNo { get; init; }
    public required string OperationName { get; init; }
    public required string OperationType { get; init; }
    public int OperationClass { get; init; }
    public string ShortName { get; init; } = "";
    public int OperationOrder { get; init; }
    public required string ProductionSegmentNo { get; init; }
    public string Description { get; init; } = "";
    public int MaterialOption { get; init; }
    public int MultiOperatorMode { get; init; }
    public bool PrintOutOnRunCard { get; init; }
    public bool SpcInspAbnormalHoldLot { get; init; }
    public decimal ErrorQtyToSpc { get; init; }
    public decimal StandardUnitRunTime { get; init; }
    public int CountOperationUnitQty { get; init; } = 1;
    public decimal StandardQueueTime { get; init; }
    public int IssueState { get; init; }
    public int DataStamp { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class OperationWriteRequest
{
    [Required, StringLength(20)] public string OperationNo { get; set; } = "";
    [Required, StringLength(200)] public string OperationName { get; set; } = "";
    [Required, StringLength(20)] public string OperationType { get; set; } = "";
    [Range(0, 9)] public int OperationClass { get; set; }
    [StringLength(40)] public string ShortName { get; set; } = "";
    public int OperationOrder { get; set; }
    [Required, StringLength(40)] public string ProductionSegmentNo { get; set; } = "";
    [StringLength(1000)] public string Description { get; set; } = "";
    public int MaterialOption { get; set; }
    public int MultiOperatorMode { get; set; }
    public bool PrintOutOnRunCard { get; set; }
    public bool SpcInspAbnormalHoldLot { get; set; }
    [Range(0, 999999)] public decimal ErrorQtyToSpc { get; set; }
    [Range(0, 999999)] public decimal StandardUnitRunTime { get; set; }
    [Range(1, 9999)] public int CountOperationUnitQty { get; set; } = 1;
    [Range(0, 999999)] public decimal StandardQueueTime { get; set; }
    public int DataStamp { get; set; }
}
