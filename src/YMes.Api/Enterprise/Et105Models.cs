using System.Text.Json.Serialization;

namespace YMes.Api.Enterprise;

public sealed class Et105RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

// Exactly the thirteen native Taker columns. Source state, stamp and audit
// nulls are returned as null, never a synthetic draft/token/timestamp.
public sealed record Et105TakerDto(string TakerNo, string? ContactorName, string? TelNo,
    string? FaxNo, string? Email, string? Address, string? BankNo, int? IssueState,
    string? Creator, DateTime? CreateDate, decimal? DataStamp, string? Reviser, DateTime? ReviseDate);

public class Et105WriteRequest
{
    [JsonRequired] public string? TakerNo { get; set; }
    public string? ContactorName { get; set; }
    public string? TelNo { get; set; }
    public string? FaxNo { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? BankNo { get; set; }
}
public sealed class Et105UpdateRequest : Et105WriteRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}
public sealed class Et105ExpectedRequest
{
    [JsonRequired] public int? ExpectedIssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}

// Internal result only. Controller sends actual204 for hard delete or the full
// saved DTO with200 for retirement; clients do not infer this from stale state.
public sealed record Et105DeleteResult(bool Deleted, Et105TakerDto? Item);
