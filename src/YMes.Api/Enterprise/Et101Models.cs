using System.Text.Json.Serialization;

namespace YMes.Api.Enterprise;

public sealed class Et101RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

// Native39 plus the two existing nullable PG extensions. No coalesced source
// state, stamp, audit or optional text. Hidden fields are read-only to this API.
public sealed record Et101CustomerDto(
    string CustomerNo, string CustomerName, string CustomerSName, string? TelNo,
    string? FaxNo, string? Www, string? TaxCode, string? Address, string? Director,
    string? Creator, DateTime? CreateDate, string? Description, int? IssueState,
    string? Address2, string? Country, string? Status, string? Currency,
    string? InvTypeNo, string? InvAddr, string? InvAddr2, string? InvCustNo,
    string? InvRemark, string? CustType, string? ArType, string? FreightTerms,
    string? PaymentTerms, string? AcctNo, string? DeliveryTerm,
    string? ForwarderAssigned, string? BrokerAssigned, string? InsuranceCoverage,
    string? Reviser, DateTime? ReviseDate, string? CustomerEName, string? ErpNo,
    decimal? DataStamp, string? SapFiskn, string? XxName1, string? XxName2,
    string? XxRebarcode, string? XxSupplydata);

// Explicit optional-null presence makes clearing different from omission.
// Audit, workflow state, extensions and external integration fields are not
// writable and unmapped input is rejected rather than silently trusted.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class Et101WriteRequest
{
    [JsonRequired] public string? CustomerNo { get; set; }
    [JsonRequired] public string? CustomerName { get; set; }
    [JsonRequired] public string? CustomerSName { get; set; }
    [JsonRequired] public string? CustomerEName { get; set; }
    [JsonRequired] public string? TelNo { get; set; }
    [JsonRequired] public string? FaxNo { get; set; }
    [JsonRequired] public string? Www { get; set; }
    [JsonRequired] public string? TaxCode { get; set; }
    [JsonRequired] public string? Address { get; set; }
    [JsonRequired] public string? Address2 { get; set; }
    [JsonRequired] public string? Director { get; set; }
    [JsonRequired] public string? Country { get; set; }
    [JsonRequired] public string? Description { get; set; }
    [JsonRequired] public string? Status { get; set; }
    [JsonRequired] public string? Currency { get; set; }
    [JsonRequired] public string? InvTypeNo { get; set; }
    [JsonRequired] public string? InvAddr { get; set; }
    [JsonRequired] public string? InvAddr2 { get; set; }
    [JsonRequired] public string? InvCustNo { get; set; }
    [JsonRequired] public string? InvRemark { get; set; }
    [JsonRequired] public string? CustType { get; set; }
    [JsonRequired] public string? ArType { get; set; }
    [JsonRequired] public string? FreightTerms { get; set; }
    [JsonRequired] public string? PaymentTerms { get; set; }
    [JsonRequired] public string? AcctNo { get; set; }
    [JsonRequired] public string? DeliveryTerm { get; set; }
    [JsonRequired] public string? ForwarderAssigned { get; set; }
    [JsonRequired] public string? BrokerAssigned { get; set; }
    [JsonRequired] public string? InsuranceCoverage { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class Et101UpdateRequest : Et101WriteRequest
{
    [JsonRequired] public int? IssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class Et101ExpectedRequest
{
    [JsonRequired] public int? IssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed record Et101DeleteResult(string Kind, string CustomerNo,
    int DeletedAddressRows, int DeletedContactorRows, Et101CustomerDto? Item);
public sealed record Et101BrokerOption(string BrokerNo, string? ContactorName, int? IssueState);
public sealed record Et101ForwarderOption(string ForwarderNo, string? ContactorName,
    string BrokerNo, int? IssueState);
