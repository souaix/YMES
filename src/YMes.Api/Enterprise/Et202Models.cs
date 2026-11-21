using System.Text.Json.Serialization;

namespace YMes.Api.Enterprise;

public sealed class Et202RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

// Exactly the eleven native TBLENTOSITEM fields. No invented IssueState or ID.
public sealed record Et202OsItemDto(string OsItemNo, string OsItemName, int PlanDays,
    string ProcessNo, int ReturnType, string? Description, string? Creator,
    DateTime? CreateDate, decimal? DataStamp, string? Reviser, DateTime? ReviseDate);

public class Et202WriteRequest
{
    [JsonRequired] public string? OsItemNo { get; set; }
    [JsonRequired] public string? OsItemName { get; set; }
    [JsonRequired] public int? PlanDays { get; set; }
    [JsonRequired] public string? ProcessNo { get; set; }
    [JsonRequired] public int? ReturnType { get; set; }
    [JsonRequired] public string? Description { get; set; }
}

public sealed class Et202UpdateRequest : Et202WriteRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed class Et202ExpectedRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed record Et202DeleteResult(string OsItemNo, int DeletedCount);
