namespace YMes.Api.Quality;

public sealed record Qc102SamplingDto(
    string SamplingType,
    int MinNum,
    int MaxNum,
    string SLevel,
    int? SampleNum,
    int? RejectNum,
    decimal? KValue,
    decimal? FValue,
    string? Creator,
    DateTime? CreateDate,
    string? Reviser,
    DateTime? ReviseDate,
    decimal? DataStamp);

public class Qc102SamplingValuesRequest
{
    public decimal? SampleNum { get; set; }
    public decimal? RejectNum { get; set; }
    public decimal? KValue { get; set; }
    public decimal? FValue { get; set; }
}

public sealed class Qc102SamplingCreateRequest : Qc102SamplingValuesRequest
{
    public string SamplingType { get; set; } = "";
    public decimal? MinNum { get; set; }
    public decimal? MaxNum { get; set; }
    public string SLevel { get; set; } = "";
}

public sealed class Qc102RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
