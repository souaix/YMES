namespace YMes.Api.Quality;

public sealed record Qc107BasisDto(string RuleNo, int ComponentCollect, int? IssueState,
    string? Creator, DateTime? CreateDate, string? Reviser, DateTime? ReviseDate,
    IReadOnlyList<string> EditableFields, string Version);

public sealed record Qc107DetailDto(string RuleNo, int Action, int ReasonType, int ReasonLevel,
    decimal? RejectNum, string? Rate, int? RuleType, string? ChangeAql,
    string? Creator, DateTime? CreateDate, string? Reviser, DateTime? ReviseDate);

public sealed record Qc107Option(int Value, string Label);
public sealed record Qc107AqlOptions(bool Enabled, IReadOnlyList<string> Items);
public sealed record Qc107SaveResult(Qc107BasisDto Basis, IReadOnlyList<Qc107DetailDto> Details);

public sealed class Qc107WriteRequest
{
    public string RuleNo { get; set; } = "";
    public int? ComponentCollect { get; set; }
    public List<Qc107DetailRequest>? Details { get; set; }
}

public sealed class Qc107DetailRequest
{
    public int? Action { get; set; }
    public int? ReasonType { get; set; }
    public int? ReasonLevel { get; set; }
    public decimal? RejectNum { get; set; }
    public string? Rate { get; set; }
    public int? RuleType { get; set; }
    public string? ChangeAql { get; set; }
}

public sealed class Qc107RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
