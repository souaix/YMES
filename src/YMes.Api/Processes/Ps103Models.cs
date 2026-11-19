using System.Text.Json.Serialization;
using MES.Api.Data;

namespace YMes.Api.Processes;

public sealed record Ps103TypeDto(string ProcessType, string? Description, int? IssueState, decimal? DataStamp,
    string? Creator, DateTime? CreateDate, string? Reviser, DateTime? ReviseDate);

public class Ps103TypeWriteRequest
{
    public string ProcessType { get; set; } = "";
    public string? Description { get; set; }
}
public sealed class Ps103TypeUpdateRequest : Ps103TypeWriteRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}
public class Ps103StampRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}
public sealed class Ps103DeleteRequest : Ps103StampRequest
{
    [JsonRequired] public int? IssueState { get; set; }
}
public sealed class Ps103RuleException(int statusCode, string message) : MasterDataRuleException(statusCode, message) { }
