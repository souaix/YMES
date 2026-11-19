using System.Text.Json.Serialization;

namespace YMes.Api.Enterprise;

public sealed class Et102RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

// Physical source order is ContactorName first; PK order is CustomerNo + name.
public sealed record Et102ContactorDto(string ContactorName, string CustomerNo, string? TelNo,
    string? FaxNo, string? Email, string? Title, string? DeptName, string? Creator,
    DateTime? CreateDate, string? Description, decimal? DataStamp, string? Reviser, DateTime? ReviseDate);
public sealed record Et102CustomerOption(string CustomerNo, string? CustomerName, string? CustomerSName, int? IssueState);

public class Et102ContactorWriteRequest
{
    [JsonRequired] public string? ContactorName { get; set; }
    public string? TelNo { get; set; }
    public string? FaxNo { get; set; }
    public string? Email { get; set; }
    public string? Title { get; set; }
    public string? DeptName { get; set; }
    public string? Description { get; set; }
}
public sealed class Et102ContactorUpdateRequest : Et102ContactorWriteRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}
public sealed class Et102ExpectedRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}
