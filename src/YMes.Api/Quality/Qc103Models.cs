using System.Text.Json.Serialization;

namespace YMes.Api.Quality;

public sealed record Qc103PlanDto(string PlanNo, string PlanName, int PlanType, string? FunctionName,
    string? Description, string? Creator, DateTime? CreateDate, int IssueState,
    int SQtyCanBBiggerThenPQty, decimal? DataStamp, string? Reviser, DateTime? ReviseDate)
{
    public IReadOnlyList<Qc103ParameterDto> Parameters { get; init; } = [];
}

public sealed record Qc103ParameterDto(string ParameterNo, string? ParameterValue, int? ShowFlag);
public sealed record Qc103PlanTypeOption(int Value, string Label);

public sealed class Qc103PlanWriteRequest
{
    public string PlanNo { get; set; } = "";
    public string PlanName { get; set; } = "";
    [JsonRequired]
    public int PlanType { get; set; }
    public string? FunctionName { get; set; }
    public string? Description { get; set; }
    [JsonRequired]
    public int SQtyCanBBiggerThenPQty { get; set; }
    [JsonRequired]
    public List<Qc103ParameterWriteRequest>? Parameters { get; set; }
}

public sealed class Qc103ParameterWriteRequest
{
    public string ParameterNo { get; set; } = "";
    public string? ParameterValue { get; set; }
    // Source ShowFlag is calculated metadata, never persisted or trusted.
}

public sealed class Qc103RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
