using System.Text.Json.Serialization;

namespace YMes.Api.Quality;

public sealed class Qc202RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed record Qc202ControlLimitDto(
    string QcItemNo,
    IReadOnlyDictionary<string, string> FactorValues,
    string? Us,
    string? Ls,
    string? Ucl,
    string? Lcl,
    string? Cl,
    string? RuleSet,
    string? Creator,
    DateTime? CreateDate,
    string? Reviser,
    DateTime? ReviseDate,
    decimal? DataStamp);

public class Qc202LimitFieldsRequest
{
    public string? QcItemNo { get; set; }
    public string? Us { get; set; }
    public string? Ls { get; set; }
    public string? Ucl { get; set; }
    public string? Lcl { get; set; }
    public string? Cl { get; set; }
    public string? RuleSet { get; set; }
}

public sealed class Qc202ControlLimitWriteRequest : Qc202LimitFieldsRequest
{
    [JsonRequired] public Dictionary<string, string>? FactorValues { get; set; }
}

public sealed class Qc202UpdateRequest : Qc202LimitFieldsRequest
{
    [JsonRequired] public Dictionary<string, string>? OriginalFactorValues { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed class Qc202DeleteRequest
{
    [JsonRequired] public Dictionary<string, string>? FactorValues { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed record Qc202FactorOptionDto(string FactorNo, string? FactorName, int? ParameterType,
    IReadOnlyList<string>? Options, bool MetadataAvailable, bool ReferenceValuesAvailable);
public sealed record Qc202RuleOptionDto(string RuleNo, string? RuleName);
public sealed record Qc202OptionsDto(
    IReadOnlyList<Qc202FactorOptionDto> Factors,
    IReadOnlyList<Qc202RuleOptionDto> Rules,
    IReadOnlyDictionary<string, IReadOnlyList<string>> FactorOptions,
    IReadOnlyList<string> MissingDependencies);
