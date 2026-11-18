namespace YMes.Api.Quality;

public sealed record Qc101SamplingTableDto(
    string SamplingTable,
    decimal MinNum,
    decimal MaxNum,
    decimal SampleNum,
    decimal? RejectNum,
    string? Creator,
    DateTime? CreateDate,
    string? Reviser,
    DateTime? ReviseDate,
    decimal? DataStamp);

public class Qc101SamplingTableValuesRequest
{
    public decimal? SampleNum { get; set; }
    public decimal? RejectNum { get; set; }
}

public sealed class Qc101SamplingTableCreateRequest : Qc101SamplingTableValuesRequest
{
    public string SamplingTable { get; set; } = "";
    public decimal? MinNum { get; set; }
    public decimal? MaxNum { get; set; }
}

public sealed class Qc101CopyRequest
{
    public string SourceSamplingTable { get; set; } = "";
    public string TargetSamplingTable { get; set; } = "";
}

public sealed record Qc101CopyResult(int InsertedCount, int SkippedCount, string? Error = null);

public sealed class Qc101RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
