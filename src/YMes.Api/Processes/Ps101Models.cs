using System.Text.Json.Serialization;

namespace YMes.Api.Processes;

public sealed record Ps101SegmentDto(
    string PsNo,
    string PsName,
    int PsOrder,
    string? Description,
    int IssueState,
    int HaveComponent,
    int HaveLevel,
    string? SerialTypeNoLot,
    string? SerialTypeNoComp,
    int IsPartial,
    int? IsNetFlow,
    decimal? DataStamp,
    string? Creator,
    DateTime? CreateDate,
    string? Reviser,
    DateTime? ReviseDate);

public sealed record Ps101SerialRuleDto(string SerialTypeNo, string? Description, int Enable);

public class Ps101SegmentWriteRequest
{
    public string PsNo { get; set; } = "";
    public string PsName { get; set; } = "";
    public decimal PsOrder { get; set; }
    public string? Description { get; set; }
    public int HaveComponent { get; set; }
    public int HaveLevel { get; set; }
    public string? SerialTypeNoLot { get; set; }
    public string? SerialTypeNoComp { get; set; }
}

public sealed class Ps101SegmentUpdateRequest : Ps101SegmentWriteRequest
{
    [JsonRequired]
    public decimal? DataStamp { get; set; }
}

public sealed class Ps101SegmentStateRequest
{
    [JsonRequired]
    public decimal? DataStamp { get; set; }
}

public sealed class Ps101RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
