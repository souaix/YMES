using System.Text.Json.Serialization;

namespace YMes.Api.Quality;

public sealed record Qc212GroupDto(
    string GroupNo, string? GroupName, int? LotIsValid, int? ComponentCollect, string? PlanNo,
    string? FormName, string? WebUrl, int? UnionErf, string? ErrorReportName, string? NoticeReportName,
    int? IssueState, string? Description,
    string? Creator, DateTime? CreateDate, string? Reviser, DateTime? ReviseDate,
    int? CursorMode, string? QcClass, string? GetFilePath, string? ExecutionFile, decimal? DataStamp);

public sealed record Qc212DetailDto(
    string GroupNo, string QcItemNo, string? QcItemName, int ShowOrder,
    bool ScriptIsValid, int? ComponentCollect, int? IssueState);

public sealed record Qc212ParameterDto(string ParameterNo, string ParameterName, int SpecType, string Value);
public sealed record Qc212ParameterOption(string ParameterNo, string ParameterName);
public sealed record Qc212VariableItemOption(string QcItemNo, string QcItemName, int ComponentCollect, int IssueState);
public sealed record Qc212OperationOption(string OpNo, string OpName, decimal? OpOrder);
public sealed record Qc212InterfaceOption(string Name, string File);
public sealed record Qc212ScriptDto(string GroupNo, string QcItemNo, string Script, string Description);

public class Qc212GroupWriteRequest
{
    public string GroupNo { get; set; } = "";
    public string GroupName { get; set; } = "";
    [JsonRequired] public int? LotIsValid { get; set; }
    [JsonRequired] public int? ComponentCollect { get; set; }
    public string? PlanNo { get; set; }
    public string? FormName { get; set; }
    public string? WebUrl { get; set; }
    [JsonRequired] public int? UnionErf { get; set; }
    public string? Description { get; set; }
    public string? ErrorReportName { get; set; }
    public string? NoticeReportName { get; set; }
    [JsonRequired] public int? CursorMode { get; set; }
    public string? QcClass { get; set; }
    public string? GetFilePath { get; set; }
    public string? ExecutionFile { get; set; }
}

public sealed class Qc212GroupUpdateRequest : Qc212GroupWriteRequest
{
    [JsonRequired] public int? IssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed class Qc212DetailReplaceRequest
{
    public List<Qc212DetailWriteRequest>? Items { get; set; }
}

public sealed class Qc212DetailWriteRequest
{
    public string QcItemNo { get; set; } = "";
    [JsonRequired] public int? ShowOrder { get; set; }
}

public sealed class Qc212CopyDetailsRequest
{
    public string TargetGroupNo { get; set; } = "";
}

public sealed class Qc212ApproveRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed class Qc212ParameterReplaceRequest
{
    public List<Qc212ParameterWriteRequest>? Items { get; set; }
}

public sealed class Qc212ParameterWriteRequest
{
    public string ParameterNo { get; set; } = "";
    public string? ParameterName { get; set; }
    [JsonRequired] public int? SpecType { get; set; }
    public string? Value { get; set; }
}

public sealed class Qc212OperationsWriteRequest
{
    [JsonRequired] public int? QcType { get; set; }
    public List<string>? OpNos { get; set; }
}

public sealed class Qc212ScriptWriteRequest
{
    public string GroupNo { get; set; } = "";
    public string QcItemNo { get; set; } = "";
    public string? Script { get; set; }
    public string? Description { get; set; }
}

public sealed class Qc212MemoWriteRequest
{
    [JsonRequired] public int? QcItemType { get; set; }
    public List<string>? ParameterNos { get; set; }
}

public sealed class Qc212RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
