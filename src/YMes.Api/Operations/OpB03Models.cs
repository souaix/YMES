using System.Text.Json.Serialization;

namespace YMes.Api.Operations;

public sealed class OpB03RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

// Exactly the eleven / twelve native columns; no workflow or derived state.
public sealed record OpB03HeaderDto(string CustomerNo, string CustomerOPNo, string CustomerOPName,
    int CustomerOPSeq, string PsNo, string? Description, string Creator, DateTime CreateDate,
    decimal? DataStamp, string? Reviser, DateTime? ReviseDate);
public sealed record OpB03DetailDto(string CustomerNo, string CustomerOPNo, string PsNo, string OpNo,
    string CustOPProcessCode, string CustStepName, int TransactionFlag, string Creator,
    DateTime CreateDate, decimal? DataStamp, string? Reviser, DateTime? ReviseDate);
public sealed record OpB03DetailsDto(OpB03HeaderDto Parent, IReadOnlyList<OpB03DetailDto> Items);
public sealed record OpB03DetailSavedDto(OpB03HeaderDto Parent, OpB03DetailDto Item);
public sealed record OpB03DetailDeletedDto(OpB03HeaderDto Parent);
public sealed record OpB03CustomerOption(string CustomerNo, string? CustomerName, int? IssueState);
public sealed record OpB03SegmentOption(string PsNo, string? PsName, int? IssueState);
public sealed record OpB03OperationOption(string OpNo, string? OpName, string? PsNo, int? IssueState);

public class OpB03HeaderWriteRequest
{
    [JsonRequired] public string? CustomerNo { get; set; }
    [JsonRequired] public string? CustomerOPNo { get; set; }
    [JsonRequired] public string? CustomerOPName { get; set; }
    [JsonRequired] public int? CustomerOPSeq { get; set; }
    [JsonRequired] public string? PsNo { get; set; }
    public string? Description { get; set; }
}
public sealed class OpB03HeaderUpdateRequest : OpB03HeaderWriteRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}
public sealed class OpB03ExpectedRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}
public class OpB03DetailWriteRequest
{
    [JsonRequired] public string? OpNo { get; set; }
    [JsonRequired] public string? CustOPProcessCode { get; set; }
    [JsonRequired] public string? CustStepName { get; set; }
    [JsonRequired] public int? TransactionFlag { get; set; }
    [JsonRequired] public decimal? ParentDataStamp { get; set; }
}
public sealed class OpB03DetailUpdateRequest : OpB03DetailWriteRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}
public sealed class OpB03DetailExpectedRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
    [JsonRequired] public decimal? ParentDataStamp { get; set; }
}
