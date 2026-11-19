using System.Text.Json.Serialization;

namespace YMes.Api.Quality;

public sealed record Qc301FactorDto(string FactorNo, string ParameterName, bool FactorSpec, int ParameterType);

public sealed record Qc301QcItemDto(
    string QcItemNo,
    string QcItemName,
    string QcDataType,
    int? ChartType,
    int SpecLineType,
    int? ComponentCollect,
    string Unit,
    int LotIsValid,
    string PlanNo,
    int SampleSize,
    IReadOnlyList<Qc301FactorDto> Factors);

public sealed class Qc301ChartBasisDto
{
    public string FactorSerial { get; set; } = "";
    public string QcItemNo { get; set; } = "";
    public string QcItemName { get; set; } = "";
    public int? IssueState { get; set; }
    public decimal? DataStamp { get; set; }
    public int SampleSize { get; set; }
    public string KeyNode { get; set; } = "N";
    public string ChartTitle { get; set; } = "";
    public string ChartUnit { get; set; } = "";
    public string Sponsor { get; set; } = "";
    public string PlanNo { get; set; } = "";
    public string Memo { get; set; } = "";
    public string Creator { get; set; } = "";
    public DateTime? CreateDate { get; set; }
    public string Reviser { get; set; } = "";
    public DateTime? ReviseDate { get; set; }
    public IReadOnlyDictionary<string, string> FactorValues { get; set; } = new Dictionary<string, string>();
    public IReadOnlyList<Qc301ChartDetailDto> Details { get; set; } = [];
    public IReadOnlyList<Qc301ChartRuleDto> Rules { get; set; } = [];
    public IReadOnlyList<Qc301SamplingPlanParameterDto> Parameters { get; set; } = [];
    public bool HasMeasurementData { get; set; }
    public bool IssueJobHasDetail { get; set; }
}

public sealed record Qc301ChartDetailDto(
    int SpcId, string ChartName, string Us, string Cs, string Ls, string Ucl, string Cl, string Lcl,
    string Target, string CpkGoal, string YAxisFrom, string YAxisTo);

public sealed record Qc301ChartRuleDto(int SpcId, string ChartName, string RuleNo, string RuleName, int Action);
public sealed record Qc301SamplingPlanParameterDto(string ParameterNo, string ParameterValue);
public sealed record Qc301SamplingPlanDto(string PlanNo, string PlanName);
public sealed record Qc301ApprovalResult(string FactorSerial, int IssueState, decimal DataStamp);
public sealed record Qc301RuleUpdateResult(string FactorSerial, decimal DataStamp, string Reviser, DateTime ReviseDate, IReadOnlyList<Qc301ChartRuleDto> Rules);
public sealed record Qc301DeleteResult(string FactorSerial, string Result, int? IssueState);
public sealed record Qc301CopyResult(int CopiedCount);

public class Qc301ChartWriteRequest
{
    public string QcItemNo { get; set; } = "";
    public int SampleSize { get; set; }
    public string KeyNode { get; set; } = "N";
    public string ChartTitle { get; set; } = "";
    public string ChartUnit { get; set; } = "";
    public string Sponsor { get; set; } = "";
    public string PlanNo { get; set; } = "";
    public string Memo { get; set; } = "";
    public Dictionary<string, string> FactorValues { get; set; } = [];
    public List<Qc301ChartDetailWriteRequest> Details { get; set; } = [];
    public List<Qc301SamplingPlanParameterDto> SamplingPlanParameters { get; set; } = [];
}

public sealed class Qc301ChartUpdateRequest : Qc301ChartWriteRequest
{
    [JsonRequired] public int? ExpectedIssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed class Qc301ChartDetailWriteRequest
{
    public int SpcId { get; set; }
    public string Us { get; set; } = "";
    public string Cs { get; set; } = "";
    public string Ls { get; set; } = "";
    public string Ucl { get; set; } = "";
    public string Cl { get; set; } = "";
    public string Lcl { get; set; } = "";
    public string Target { get; set; } = "";
    public string CpkGoal { get; set; } = "";
    public string YAxisFrom { get; set; } = "";
    public string YAxisTo { get; set; } = "";
}

public sealed class Qc301StateRequest
{
    [JsonRequired] public int? IssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed class Qc301RulesRequest
{
    [JsonRequired] public int? ExpectedIssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
    [JsonRequired] public List<Qc301RuleWriteRequest>? Rules { get; set; }
}

public sealed class Qc301RuleWriteRequest
{
    public int SpcId { get; set; }
    public string RuleNo { get; set; } = "";
    public int Action { get; set; }
}

public sealed class Qc301CopyRequest
{
    public string FromQcItemNo { get; set; } = "";
    public string ToQcItemNo { get; set; } = "";
    public int CopyType { get; set; }
}

public sealed class Qc301RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
