using System.ComponentModel.DataAnnotations;

namespace MES.Contracts;

public sealed record MaterialDto
{
    public required string MaterialNo { get; init; }
    public required string MaterialType { get; init; }
    public required string MaterialName { get; init; }
    public string MaterialSpec { get; init; } = "";
    public string Description { get; init; } = "";
    public string PutInPlace { get; init; } = "";
    public int CountWay { get; init; }
    public int CheckLotNo { get; init; }
    public string UnitType { get; init; } = "";
    public required string UnitNo { get; init; }
    public decimal SafeQty { get; init; }
    public bool KeyMaterials { get; init; }
    public bool OpenLimitTimeFlag { get; init; }
    public decimal? OpenLimitTime { get; init; }
    public bool ResetOpenDate { get; init; }
    public bool ExpireDateFlag { get; init; }
    public string ErpNo { get; init; } = "";
    public int IssueState { get; init; }
    public int DataStamp { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class MaterialWriteRequest
{
    [Required, StringLength(50)] public string MaterialNo { get; set; } = "";
    [Required, StringLength(50)] public string MaterialType { get; set; } = "";
    [Required, StringLength(300)] public string MaterialName { get; set; } = "";
    [StringLength(300)] public string MaterialSpec { get; set; } = "";
    [StringLength(255)] public string Description { get; set; } = "";
    [Required, StringLength(40)] public string PutInPlace { get; set; } = "";
    [Range(-9, 9)] public int CountWay { get; set; }
    [Range(-9, 9)] public int CheckLotNo { get; set; }
    [StringLength(30)] public string UnitType { get; set; } = "";
    [Required, StringLength(30)] public string UnitNo { get; set; } = "";
    [Range(typeof(decimal), "0", "99999999.99999999")] public decimal SafeQty { get; set; }
    public bool KeyMaterials { get; set; }
    public bool OpenLimitTimeFlag { get; set; }
    [Range(typeof(decimal), "0", "9999.99")] public decimal? OpenLimitTime { get; set; }
    public bool ResetOpenDate { get; set; }
    public bool ExpireDateFlag { get; set; }
    [StringLength(40)] public string ErpNo { get; set; } = "";
    public int DataStamp { get; set; }
}
