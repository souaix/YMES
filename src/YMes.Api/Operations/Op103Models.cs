using System.Text.Json.Serialization;

namespace YMes.Api.Operations;

public sealed record Op103TypeDto(string OpType, string? Description, string? Creator, DateTime? CreateDate,
    int IssueState, decimal? DataStamp, string? Reviser, DateTime? ReviseDate);
public class Op103WriteRequest
{
    public string OpType { get; set; } = "";
    public string? Description { get; set; }
}
public sealed class Op103UpdateRequest : Op103WriteRequest
{
    [JsonRequired] public int? IssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}
public sealed class Op103CommandRequest
{
    [JsonRequired] public int? IssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}
public sealed record Op103DeleteResult(string Outcome, Op103TypeDto Item);
