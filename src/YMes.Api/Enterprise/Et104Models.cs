using System.Text.Json.Serialization;

namespace YMes.Api.Enterprise;

public sealed class Et104RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

// All fifteen native source fields, preserving raw nullable contact/audit/state/token.
public sealed record Et104ForwarderDto(string ForwarderNo, string? ContactorName, string? TelNo,
    string? FaxNo, string? Email, string? Address, string BrokerNo, string? BankNo, int? IssueState,
    string? Creator, DateTime? CreateDate, string? AcNo, decimal? DataStamp, string? Reviser, DateTime? ReviseDate);

public class Et104WriteRequest
{
    [JsonRequired] public string? ForwarderNo { get; set; }
    public string? ContactorName { get; set; }
    public string? TelNo { get; set; }
    public string? FaxNo { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    [JsonRequired] public string? BrokerNo { get; set; }
    public string? BankNo { get; set; }
    public string? AcNo { get; set; }
}
public sealed class Et104UpdateRequest : Et104WriteRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}
public sealed class Et104ExpectedRequest
{
    [JsonRequired] public int? ExpectedIssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed record Et104DeleteResult(bool Deleted, Et104ForwarderDto? Item);
