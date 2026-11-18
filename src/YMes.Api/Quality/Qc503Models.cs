using System.Text.Json.Serialization;

namespace YMes.Api.Quality;

public sealed record Qc503DriverDto(
    string InspDriverNo,
    string? FunctionName,
    string? Description,
    string? Creator,
    DateTime? CreateDate,
    int IssueState,
    decimal? DataStamp,
    string? Reviser,
    DateTime? ReviseDate);

public class Qc503DriverWriteRequest
{
    public string InspDriverNo { get; set; } = "";
    public string? FunctionName { get; set; }
    public string? Description { get; set; }
}

public sealed class Qc503DriverUpdateRequest : Qc503DriverWriteRequest
{
    public int IssueState { get; set; }
    [JsonRequired]
    public decimal? DataStamp { get; set; }
}

public sealed class Qc503DriverStateRequest
{
    public int IssueState { get; set; }
    [JsonRequired]
    public decimal? DataStamp { get; set; }
}

public sealed class Qc503RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
