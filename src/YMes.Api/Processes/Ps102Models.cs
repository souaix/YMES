using System.Text.Json.Serialization;

namespace YMes.Api.Processes;

public sealed record Ps102StageDto(
    string StageNo,
    string StageName,
    string StageType,
    int Sequence,
    int? IssueState,
    string? Creator,
    DateTime? CreateDate,
    decimal? DataStamp,
    string? Reviser,
    DateTime? ReviseDate);

public class Ps102StageWriteRequest
{
    public string StageNo { get; set; } = "";
    public string StageName { get; set; } = "";
    public string StageType { get; set; } = "";
    public decimal Sequence { get; set; }
}

public sealed class Ps102StageUpdateRequest : Ps102StageWriteRequest
{
    [JsonRequired]
    public decimal? DataStamp { get; set; }
}

public sealed class Ps102StageStateRequest
{
    [JsonRequired]
    public decimal? DataStamp { get; set; }
}

public sealed class Ps102RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
