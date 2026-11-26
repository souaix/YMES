using System.Text.Json.Serialization;

namespace YMes.Api.Quality;

public sealed record Qc213GroupDto(
    string GroupNo, string GroupName, int LotIsValid, string? Description, int IssueState,
    string? Creator, DateTime? CreateDate, string? Reviser, DateTime? ReviseDate, decimal DataStamp);

public class Qc213GroupWriteRequest
{
    public string GroupNo { get; set; } = "";
    public string GroupName { get; set; } = "";
    [JsonRequired] public int? LotIsValid { get; set; }
    public string? Description { get; set; }
}

public sealed class Qc213GroupUpdateRequest : Qc213GroupWriteRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed class Qc213CommandRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed class Qc213DetailReplaceRequest
{
    [JsonRequired] public List<Qc213DetailWriteRequest>? Items { get; set; }
}

public sealed class Qc213DetailWriteRequest
{
    public string QcItemNo { get; set; } = "";
    [JsonRequired] public int? ShowOrder { get; set; }
}

public sealed record Qc213DetailDto(string GroupNo, string QcItemNo, string QcItemName, int ShowOrder);
public sealed record Qc213AttributeItemOption(string QcItemNo, string QcItemName, int IssueState);

public sealed class Qc213OperationReplaceRequest
{
    [JsonRequired] public List<Qc213OperationWriteRequest>? Operations { get; set; }
}

public sealed class Qc213OperationWriteRequest
{
    public string OpNo { get; set; } = "";
    public bool Inline { get; set; }
    public bool InspTypeFai { get; set; }
    public bool InspTypeIpqc { get; set; }
    public bool InspTypePqc { get; set; }
}

public sealed record Qc213OperationDto(
    string OpNo, string? OpName, int Inline, bool InspTypeFai, bool InspTypeIpqc,
    bool InspTypePqc, int ExecuteOrder);

public sealed class Qc213RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
