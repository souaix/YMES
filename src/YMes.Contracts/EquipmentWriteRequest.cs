using System.ComponentModel.DataAnnotations;

namespace MES.Contracts;

public sealed class EquipmentWriteRequest
{
    [Required, StringLength(50)]
    public string EquipmentNo { get; set; } = "";

    [Required, StringLength(50)]
    public string EquipmentType { get; set; } = "";

    [Required, StringLength(255)]
    public string EquipmentName { get; set; } = "";

    [Required, StringLength(20)]
    public string EngineerGroupNo { get; set; } = "";

    [Required, StringLength(50)]
    public string VendorNo { get; set; } = "N/A";

    [StringLength(50)]
    public string ModelNo { get; set; } = "";

    [StringLength(50)]
    public string AssetNo { get; set; } = "";

    [StringLength(50)]
    public string EquipmentClass { get; set; } = "";

    [StringLength(4)]
    public string EquipmentClassNo { get; set; } = "";

    [StringLength(255)]
    public string Description { get; set; } = "";

    [Range(-1, 99999999)]
    public decimal Capacity { get; set; } = -1;
    [Range(-1, 99)] public int LoadPort { get; set; }
    [Range(0, 1)] public int AutoFlag { get; set; }

    [StringLength(200)]
    public string EAController { get; set; } = "N/A";

    [Range(0, 1)] public int EqpRecipe { get; set; }

    [StringLength(50)]
    public string QCListNo { get; set; } = "";

    [Range(0, 9999.99)] public decimal? MaxTime { get; set; }
    [Range(0, 9999.99)] public decimal? FixEqpTime { get; set; }
    [Range(0, 9999.99)] public decimal? VarEqpTime { get; set; }
    [Range(0, 999999)] public int CountEqpUnitQty { get; set; } = 1;
    [Range(0, 999999)] public int CountEquipmentUnitQty { get; set; } = 1;

    [StringLength(50)] public string QueueServer { get; set; } = "";
    [StringLength(100)] public string RequestMsgQueue { get; set; } = "";
    [StringLength(100)] public string ResponseMsgQueue { get; set; } = "";
    [StringLength(100)] public string RequestMsgQueueMes { get; set; } = "";
    [StringLength(100)] public string ResponseMsgQueueEap { get; set; } = "";

    public long DataStamp { get; set; }
}

public sealed class ChamberWriteRequest
{
    [Required, StringLength(50)] public string EquipmentNo { get; set; } = "";
    [Required, StringLength(50)] public string BelongEquipmentNo { get; set; } = "";
    [Required, StringLength(255)] public string EquipmentName { get; set; } = "";
    [StringLength(255)] public string Description { get; set; } = "";
    public long DataStamp { get; set; }
}
