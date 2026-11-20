using System.Text.Json.Serialization;

namespace YMes.Api.Enterprise;

public sealed class Et106RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed record Et106CustomerOption(string CustomerNo, string? CustomerName, string? CustomerSName, int? IssueState);
public sealed record Et106AddressDto(string ShipAddr, string CustomerNo, string? TelNo, string? ContactorName,
    string? ShipCustomerName, string? Creator, DateTime? CreateDate, string? Description, string Flow,
    string DeclarationFlag, string? OverseaFlag, string? FaxNo, string? Email, string? ShipAddr2,
    string? Country, string? CompanyName, string? TakerNo, string? ForwarderNo, string? Broker,
    decimal? DataStamp, string? Reviser, DateTime? ReviseDate);

public class Et106AddressWriteRequest
{
    [JsonRequired] public string? ShipAddr { get; set; }
    public string? TelNo { get; set; }
    public string? ContactorName { get; set; }
    [JsonRequired] public string? ShipCustomerName { get; set; }
    public string? Description { get; set; }
    [JsonRequired] public string? Flow { get; set; }
    [JsonRequired] public string? DeclarationFlag { get; set; }
    public string? OverseaFlag { get; set; }
    public string? FaxNo { get; set; }
    public string? Email { get; set; }
    public string? ShipAddr2 { get; set; }
    public string? Country { get; set; }
    public string? CompanyName { get; set; }
    public string? TakerNo { get; set; }
    public string? ForwarderNo { get; set; }
    public string? Broker { get; set; }
}
public sealed class Et106AddressUpdateRequest : Et106AddressWriteRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}
public sealed class Et106AddressExpectedRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
    [JsonRequired] public string? ExpectedFlow { get; set; }
    [JsonRequired] public string? ExpectedShipCustomerName { get; set; }
    [JsonRequired] public string? ExpectedDeclarationFlag { get; set; }
}

public sealed record Et106DeclarationDto(string CustomerNo, string ShipTo, string Flow, string OverseaFlag,
    string DeclarationFlag, string BondFlag, string HandCarryFlag, string DeclarationWay,
    string? Creator, DateTime? CreateDate, decimal? DataStamp, string? Reviser, DateTime? ReviseDate);
public sealed record Et106DeclarationKey(
    [property: JsonRequired] string ShipTo, [property: JsonRequired] string Flow,
    [property: JsonRequired] string OverseaFlag, [property: JsonRequired] string DeclarationFlag,
    [property: JsonRequired] string BondFlag, [property: JsonRequired] string HandCarryFlag);
public sealed record Et106DeclarationGroup(Et106DeclarationKey Key, string AggregateVersion, int RowCount);
public sealed record Et106DeclarationList(IReadOnlyList<Et106DeclarationDto> Items, IReadOnlyList<Et106DeclarationGroup> Groups);
public sealed record Et106DeclarationSaved(Et106DeclarationGroup Group, IReadOnlyList<Et106DeclarationDto> Items, int AffectedRows);
public sealed record Et106DeclarationCreated(Et106DeclarationDto Inserted, Et106DeclarationGroup Group,
    IReadOnlyList<Et106DeclarationDto> Items, int AffectedRows);
public sealed record Et106DeclarationDeleted(Et106DeclarationKey Key, int RowCount,
    IReadOnlyList<Et106DeclarationDto> Items, string? AggregateVersion, int AffectedRows);
public sealed class Et106DeclarationWriteRequest
{
    [JsonRequired] public string? ShipTo { get; set; }
    [JsonRequired] public string? Flow { get; set; }
    [JsonRequired] public string? OverseaFlag { get; set; }
    [JsonRequired] public string? DeclarationFlag { get; set; }
    [JsonRequired] public string? BondFlag { get; set; }
    [JsonRequired] public string? HandCarryFlag { get; set; }
    [JsonRequired] public string? DeclarationWay { get; set; }
    [JsonRequired] public string? SourceShipAddr { get; set; }
}
public class Et106DeclarationExpectedRequest
{
    [JsonRequired] public Et106DeclarationKey? Key { get; set; }
    [JsonRequired] public string? ExpectedGroupVersion { get; set; }
}
public sealed class Et106DeclarationUpdateRequest : Et106DeclarationExpectedRequest
{
    [JsonRequired] public string? DeclarationWay { get; set; }
}
