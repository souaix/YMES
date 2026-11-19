using System.Text.Json.Serialization;

namespace YMes.Api.Quality;

public sealed class Qc201RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed record Qc201ItemDto(
    string QcItemNo, string? QcItemName, int? LotIsValid, int? ComponentCollect,
    string? PlanNo, string? RuleNo, string? FormName, string? Description,
    int? LimitLotNum, int? IsSampleSizeInteger, string? QcClass, string? MClassNo,
    int? IssueState, decimal? DataStamp, string? Creator, DateTime? CreateDate,
    string? Reviser, DateTime? ReviseDate, string? ExecutionFile);

public class Qc201ItemWriteRequest
{
    public string QcItemNo { get; set; } = "";
    public string? QcItemName { get; set; }
    [JsonRequired] public int? LotIsValid { get; set; }
    [JsonRequired] public int? ComponentCollect { get; set; }
    public string? PlanNo { get; set; }
    public string? RuleNo { get; set; }
    public string? FormName { get; set; }
    public string? Description { get; set; }
    [JsonRequired] public int? LimitLotNum { get; set; }
    public int? IsSampleSizeInteger { get; set; } = 0;
    public string? QcClass { get; set; }
    public string? MClassNo { get; set; }
}

public sealed class Qc201UpdateRequest : Qc201ItemWriteRequest
{
    [JsonRequired] public int? IssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed class Qc201CopyWriteRequest : Qc201ItemWriteRequest
{
    [JsonRequired] public int? IssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
    [JsonRequired] public bool CopyParameters { get; set; }
    [JsonRequired] public bool CopyFactors { get; set; }
    [JsonRequired] public bool CopyReasons { get; set; }
    [JsonRequired] public bool CopySamplePlanChanges { get; set; }
    public string? ParameterSource { get; set; }
    public string? SourceVersion { get; set; }
}

public class Qc201CommandRequest
{
    [JsonRequired] public int? IssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed class Qc201DeleteRequest : Qc201CommandRequest { }
public sealed class Qc201ApproveRequest : Qc201CommandRequest { }

public sealed class Qc201RowsRequest<T> : Qc201CommandRequest
{
    [JsonRequired] public string? Version { get; set; }
    [JsonRequired] public List<T>? Rows { get; set; }
}

public sealed record Qc201ChildSnapshot<T>(string QcItemNo, int? IssueState, decimal? DataStamp,
    string Version, IReadOnlyList<T> Items, bool Writable, string? WriteUnavailableReason);

public sealed record Qc201CopyPreview(string SourceQcItemNo, int? IssueState, decimal? DataStamp,
    string SourceVersion, bool ParametersDiffer, IReadOnlyList<Qc201ParameterRow> ItemParameters,
    IReadOnlyList<Qc201ParameterRow> PlanParameters, IReadOnlyList<string> ParameterSources);

public sealed class Qc201ParameterRow
{
    public string ParameterNo { get; set; } = "";
    public string? ParameterValue { get; set; }
}

public sealed class Qc201FactorRow
{
    public string FactorNo { get; set; } = "";
    public string? FactorName { get; set; }
    public int? ParameterType { get; set; }
    public bool MetadataAvailable { get; set; }
    public string? MetadataUnavailableReason { get; set; }
    public string? FactorOnline { get; set; }
    public string? FactorSpec { get; set; }
    public string? OpNo { get; set; }
    public string? TableName { get; set; }
    public string? FieldName { get; set; }
    public string? FactorShortRun { get; set; }
}

public sealed class Qc201ReasonRow
{
    public string ReasonNo { get; set; } = "";
}

public sealed record Qc201ReasonDto(string ReasonNo, string ReasonName, int ReasonType,
    int ReasonLevel, int IssueState);

public sealed record Qc201ReasonMapDto(string QcItemNo, string ReasonNo, string? ReasonName,
    string? Creator, DateTime? CreateDate, string? Reviser, DateTime? ReviseDate);

public sealed class Qc201SamplePlanChangeRow
{
    public string OldSamplePlan { get; set; } = "";
    public int AccLotQty { get; set; }
    public int RejLotQty { get; set; }
    public string? NewSamplePlan { get; set; }
    public string FactorNo { get; set; } = "";
    public int LotRange { get; set; }
    public int? AqlChkForSPlanChg { get; set; }
    public int? QcItemType { get; set; }
}

public sealed class Qc201OperationRow
{
    public string OpNo { get; set; } = "";
    public string? OpName { get; set; }
    public int Inline { get; set; }
    public int ExecuteOrder { get; set; }
    public int QcItemType { get; set; }
    public bool? InspTypeFai { get; set; }
    public bool? InspTypeIpqc { get; set; }
    public bool? InspTypePqc { get; set; }
    public string? Reviser { get; set; }
    public DateTime? ReviseDate { get; set; }
}

public sealed record Qc201Availability(bool Available, string? Detail);
public sealed record Qc201OptionDto(IReadOnlyList<string>? SamplingPlans,
    IReadOnlyList<string>? AttributeRules, IReadOnlyList<string>? MClasses,
    IReadOnlyList<string>? Interfaces, bool ShowQcClass, bool ShowLotValid,
    bool ShowComponentCollect, IReadOnlyDictionary<string, Qc201Availability> Availability,
    IReadOnlyList<string> MissingDependencies);
public sealed record Qc201FactorReference(string FactorNo, int ParameterType,
    bool FreeInput, IReadOnlyList<string>? Items);
public sealed record Qc201FactorChoice(string FactorNo, string FactorName, int ParameterType, bool FreeInput);
public sealed record Qc201ValueOption(string Value, string Label);
public sealed class Qc201SimulationRequest { public string? PlanNo { get; set; } }
