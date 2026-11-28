namespace YMes.Api.Wip;

public sealed class Wp305EquipmentLookupQuery
{
    public string? EquipmentType { get; set; }
    public string? Keyword { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed class Wp305EquipmentTypeQuery
{
    public string? Keyword { get; set; }
}

public sealed class Wp305CandidateQuery
{
    public string? EquipmentNo { get; set; }
    public string? Mode { get; set; }
    public bool CheckIn { get; set; }
    public string? LotSerial { get; set; }
    public string? LogGroupSerial { get; set; }
}

public sealed class Wp305QcListQuery
{
    public string? EquipmentNo { get; set; }
    public string? QcTaskNo { get; set; }
    public string? Mode { get; set; }
    public bool CheckIn { get; set; }
    public string? LotSerial { get; set; }
    public string? LogGroupSerial { get; set; }
}

public sealed class Wp305InspectionSaveRequest
{
    public string? RequestId { get; set; }
    public string? EquipmentNo { get; set; }
    public string? QcListNo { get; set; }
    public string? QcTaskNo { get; set; }
    public string? Mode { get; set; }
    public bool CheckIn { get; set; }
    public string? LotSerial { get; set; }
    public string? LogGroupSerial { get; set; }
    public int? InspType { get; set; }
    public string? InspStartTime { get; set; }
    public bool AllowPartial { get; set; }
    public string? Description { get; set; }
    public IReadOnlyList<Wp305InspectionInput>? Details { get; set; }
}

public sealed class Wp305InspectionInput
{
    public decimal QcOrder { get; set; }
    public string? InputValue { get; set; }
    public IReadOnlyList<string?>? Readings { get; set; }
    public string? MessageChoice { get; set; }
}

public sealed record Wp305EquipmentTypeItem(string EquipmentType);

public sealed record Wp305EquipmentItem(string EquipmentNo, string EquipmentType, string? EquipmentName);

public sealed record Wp305Page<T>(int Page, int PageSize, bool HasMore, IReadOnlyList<T> Items);

public sealed record Wp305Candidate(
    string? QcTaskNo,
    string QcListNo,
    string QcListName,
    int InspType,
    string InspStartTime,
    DateTime? PlanQcTime,
    string? ShiftNo,
    string? LotNo,
    int? Status);

public sealed record Wp305QcList(
    string QcListNo,
    string QcListName,
    int QcType,
    int? UnValidAction,
    string? Description,
    int AfterEventQc,
    IReadOnlyList<Wp305QcListDetail> Details);

public sealed record Wp305QcListDetail(
    decimal QcOrder,
    string QcItem,
    int QcType,
    int InputDataCount,
    string? StdValue,
    string? MiniValue,
    string? MaxiValue,
    string? InspectionMethod,
    string? InspectionStandards);

public sealed record Wp305InspectionSaveResult(
    string QcListSerial,
    string EquipmentNo,
    string QcListNo,
    string? QcTaskNo,
    int QcResult,
    DateTime CreateDate,
    bool Replayed);

public sealed class Wp305EquipmentOnlineQcRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
