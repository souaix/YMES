namespace MES.Contracts;

public sealed record MaterialPropertyDto(string PropertyNo, string Value, int Sequence, int IssueState, int Controlled, string PropertyName, int DataStamp);
public sealed record MaterialPropertyWriteRequest(string PropertyNo, string? Value, int Sequence, int DataStamp = 0);
public sealed record MaterialQCItemDto(string QCItemNo, int QCItemType, string QCItemName = "");
public sealed record MaterialQCItemsDto(IReadOnlyList<MaterialQCItemDto> Available, IReadOnlyList<MaterialQCItemDto> Assigned);
public sealed record MaterialQCItemsWriteRequest(IReadOnlyList<MaterialQCItemDto> Items);
public sealed record MaterialVendorDto(string MaterialNo, string VendorNo, string VendorName, string Description, int DataStamp);
public sealed record MaterialVendorWriteRequest(string VendorNo, string? Description, int DataStamp = 0);
public sealed record MaterialGumHourDto(string MaterialType, string MaterialNo, decimal AccMin, decimal AccMax, bool RefrozenFlag, decimal? FrozenMin, int ReForzenTimes, bool ReacclimateFlag, decimal? ReAccMin, decimal? ReAccMax, int DataStamp);
public sealed record MaterialGumHourWriteRequest(decimal AccMin, decimal AccMax, bool RefrozenFlag, decimal? FrozenMin, int ReForzenTimes, bool ReacclimateFlag, decimal? ReAccMin, decimal? ReAccMax, int DataStamp = 0);
public sealed record MaterialImportRowRequest(string MaterialNo, string MaterialName, string MaterialType, string UnitNo, string UnitType = "", string MaterialSpec = "", string Description = "", string PutInPlace = "0", int CountWay = 0, int CheckLotNo = 0);
public sealed record MaterialImportRowResult(int Row, string MaterialNo, bool Success, string Message);
public sealed record MaterialOptionsDto(IReadOnlyList<MaterialOptionDto> MaterialTypes, IReadOnlyList<MaterialOptionDto> Units, IReadOnlyList<MaterialOptionDto> UnitTypes, IReadOnlyList<MaterialOptionDto> PutInPlace, IReadOnlyList<MaterialOptionDto> CountWay, IReadOnlyList<MaterialOptionDto> CheckLotNo);
public sealed record MaterialOptionDto(string Value, string Label, string UnitType = "");
