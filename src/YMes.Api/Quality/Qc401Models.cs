namespace YMes.Api.Quality;

public sealed record Qc401MClassDto(
    string MClassNo,
    string? Description,
    string? Creator,
    DateTime? CreateDate,
    int IssueState,
    decimal? DataStamp,
    string? Reviser,
    DateTime? ReviseDate);

public sealed class Qc401MClassWriteRequest
{
    public string MClassNo { get; set; } = "";
    public string? Description { get; set; }
}

public sealed record Qc401ImportPair(string MClassNo, string Description);
public sealed record Qc401ImportResult(string MClassNo, string Description, string Result, string Message);

public sealed class Qc401RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
