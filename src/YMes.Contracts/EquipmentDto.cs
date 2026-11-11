namespace MES.Contracts;

public sealed record EquipmentDto
{
    public required string EquipmentNo { get; init; }
    public required string EquipmentType { get; init; }
    public required string EquipmentName { get; init; }
    public required string EngineerGroupNo { get; init; }
    public required string VendorNo { get; init; }
    public string ModelNo { get; init; } = "";
    public string AssetNo { get; init; } = "";
    public string EquipmentClass { get; init; } = "";
    public string EquipmentClassNo { get; init; } = "";
    public string Description { get; init; } = "";
    public decimal Capacity { get; init; }
    public int LoadPort { get; init; }
    public int AutoFlag { get; init; }
    public string EAController { get; init; } = "N/A";
    public int EqpRecipe { get; init; }
    public string QCListNo { get; init; } = "";
    public decimal? MaxTime { get; init; }
    public decimal? FixEqpTime { get; init; }
    public decimal? VarEqpTime { get; init; }
    public int CountEqpUnitQty { get; init; } = 1;
    public int CountEquipmentUnitQty { get; init; } = 1;
    public string QueueServer { get; init; } = "";
    public string RequestMsgQueue { get; init; } = "";
    public string ResponseMsgQueue { get; init; } = "";
    public string RequestMsgQueueMes { get; init; } = "";
    public string ResponseMsgQueueEap { get; init; } = "";
    public bool ChamberFlag { get; init; }
    public string? BelongEquipmentNo { get; init; }
    public int IssueState { get; init; }
    public long DataStamp { get; init; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? RevisedAt { get; init; }
}
