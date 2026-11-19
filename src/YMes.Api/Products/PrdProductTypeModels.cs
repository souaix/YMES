using System.Text.Json.Serialization;

namespace YMes.Api.Products;

public sealed record PrdProductTypeDto(string ProductType, string? Description, string? Creator,
    DateTime? CreateDate, int IssueState, string? ExPrdtype1, string? ExPrdtype2,
    string? ProductTypeCode, decimal? DataStamp, string? Reviser, DateTime? ReviseDate);

public class PrdProductTypeWriteRequest
{
    public string ProductType { get; set; } = "";
    public string? ProductTypeCode { get; set; }
    public string? Description { get; set; }
}

public sealed class PrdProductTypeUpdateRequest : PrdProductTypeWriteRequest
{
    [JsonRequired] public int? IssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed class PrdProductTypeCommandRequest
{
    [JsonRequired] public int? IssueState { get; set; }
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed record PrdProductTypeDeleteResult(string Outcome, PrdProductTypeDto Item);
