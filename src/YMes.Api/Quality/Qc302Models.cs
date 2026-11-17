using System.Text.Json;

namespace YMes.Api.Quality;

public sealed record Qc302PreviewResponse(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyDictionary<string, string>> Rows);

public sealed record Qc302ImportRequest(List<Dictionary<string, JsonElement>>? Rows);

public sealed record Qc302ImportRowResult(int RowNo, string QcItemNo, bool Succeeded, string Execute, string FactorSerial);

public sealed class Qc302RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
