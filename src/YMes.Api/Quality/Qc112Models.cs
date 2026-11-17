namespace YMes.Api.Quality;

public sealed class Qc112RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed record Qc112ReasonDto(
    string ReasonNo,
    string ReasonName,
    int ReasonLevel,
    int ReasonType,
    string? ReasonSubType,
    string? Description,
    string? Creator,
    DateTime? CreateDate,
    string? Reviser,
    DateTime? ReviseDate,
    int IssueState,
    decimal? DataStamp);

public sealed record Qc112ProductDto(
    string ProductNo,
    string ProductName,
    int IssueState,
    bool CurrentVersion);

public sealed record Qc112SopFileDto(
    string ReasonNo,
    string ProductNo,
    string SopFileName,
    string DisplayPath);

public sealed record Qc112ImportPreviewRow(
    int RowNumber,
    string ReasonNo,
    string ProductNo,
    string SopFileName,
    string SopFilePath,
    string Result,
    string Message);
