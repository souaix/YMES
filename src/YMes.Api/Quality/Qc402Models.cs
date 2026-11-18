namespace YMes.Api.Quality;

public sealed record Qc402SClassDto(
    string SClassNo,
    string? MClassNo,
    string? Description,
    string? Creator,
    DateTime? CreateDate,
    int IssueState,
    decimal? DataStamp,
    string? Reviser,
    DateTime? ReviseDate);

public sealed record Qc402MClassOption(string MClassNo, string? Description, int IssueState);

public sealed class Qc402SClassWriteRequest
{
    public string SClassNo { get; set; } = "";
    public string MClassNo { get; set; } = "";
    public string? Description { get; set; }
}

public sealed record Qc402ImportPair(string SClassNo, string MClassNo, string Description);
public sealed record Qc402ImportResult(string SClassNo, string MClassNo, string Description, string Result, string Message);

public sealed class Qc402RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
