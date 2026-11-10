using System.ComponentModel.DataAnnotations;

namespace MES.Contracts;

public sealed class EquipmentWriteRequest
{
    [Required, StringLength(40)]
    public string EquipmentNo { get; set; } = "";

    [Required, StringLength(40)]
    public string EquipmentType { get; set; } = "";

    [Required, StringLength(200)]
    public string EquipmentName { get; set; } = "";

    [Required, StringLength(40)]
    public string EngineerGroupNo { get; set; } = "";

    [Required, StringLength(40)]
    public string VendorNo { get; set; } = "N/A";

    [StringLength(80)]
    public string ModelNo { get; set; } = "";

    [StringLength(80)]
    public string AssetNo { get; set; } = "";

    [StringLength(40)]
    public string EquipmentClass { get; set; } = "";

    [StringLength(40)]
    public string EquipmentClassNo { get; set; } = "";

    [StringLength(1000)]
    public string Description { get; set; } = "";

    public decimal Capacity { get; set; } = -1;
    [Range(-1, 999)] public int LoadPort { get; set; }
    [Range(0, 1)] public int AutoFlag { get; set; }
    public bool DefaultServer { get; set; }

    [StringLength(80)]
    public string EAController { get; set; } = "N/A";

    [Range(0, 1)] public int EqpRecipe { get; set; }

    [StringLength(40)]
    public string QCListNo { get; set; } = "";

    [Range(0, 24)] public decimal? MaxTime { get; set; }
    [Range(0, 100000)] public decimal? FixEqpTime { get; set; }
    [Range(0, 100000)] public decimal? VarEqpTime { get; set; }
    [Range(0, 9999)] public int CountEqpUnitQty { get; set; } = 1;
    [Range(0, 9999)] public int CountEquipmentUnitQty { get; set; } = 1;

    [StringLength(100)] public string QueueServer { get; set; } = "";
    [StringLength(200)] public string RequestMsgQueue { get; set; } = "";
    [StringLength(200)] public string ResponseMsgQueue { get; set; } = "";
    [StringLength(200)] public string RequestMsgQueueMes { get; set; } = "";
    [StringLength(200)] public string ResponseMsgQueueEap { get; set; } = "";

    public int DataStamp { get; set; }
}

public sealed class ChamberWriteRequest
{
    [Required, StringLength(40)] public string EquipmentNo { get; set; } = "";
    [Required, StringLength(40)] public string BelongEquipmentNo { get; set; } = "";
    [Required, StringLength(200)] public string EquipmentName { get; set; } = "";
    [StringLength(1000)] public string Description { get; set; } = "";
    public int DataStamp { get; set; }
}
