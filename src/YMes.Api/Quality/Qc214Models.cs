using System.Text.Json;

namespace YMes.Api.Quality;

public sealed class Qc214RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class Qc214ImportRequest
{
    public List<Dictionary<string, JsonElement>>? Rows { get; set; }
}

public sealed record Qc214PreviewResponse(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Rows);

public sealed record Qc214ImportRowResult(
    int RowNo,
    string QcItemNo,
    bool Succeeded,
    int Result,
    string Execute);
