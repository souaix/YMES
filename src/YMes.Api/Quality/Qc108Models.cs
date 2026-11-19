namespace YMes.Api.Quality;

public sealed record Qc108RuleDto(
    string RuleSerial, string RuleNo, string RuleName, string? FunctionName,
    int? RuleType, int? IssueState, string? Creator, DateTime? CreateDate, string? Description,
    int? RuleOption, int? TestOutof, int? TestCount, string? CompareValue1, string? CompareValue2,
    string? Relation, int? WeType, decimal? DataStamp, string? Reviser, DateTime? ReviseDate,
    IReadOnlyList<string> EditableFields);

public sealed class Qc108RuleWriteRequest
{
    public string RuleNo { get; set; } = "";
    public string RuleName { get; set; } = "";
    public int? RuleType { get; set; }
    public string? FunctionName { get; set; }
    public string? Description { get; set; }
    public int? RuleOption { get; set; }
    public decimal? TestOutof { get; set; }
    public decimal? TestCount { get; set; }
    public string? CompareValue1 { get; set; }
    public string? CompareValue2 { get; set; }
    public string? Relation { get; set; }
}

public sealed record Qc108RuleOption(int Value, string Label);
public sealed record Qc108ExpectedVersion(int IssueState, decimal? DataStamp);

public sealed class Qc108RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
