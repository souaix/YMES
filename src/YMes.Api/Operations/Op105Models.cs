using System.Text.Json.Serialization;

namespace YMes.Api.Operations;

public sealed class Op105RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed record Op105GroupDto(string OpGroupNo, string OpGroupName, string? PsNo, int? OpGroupOrder,
    string? Description, string? ErpNo, string? Creator, DateTime? CreateDate, string? Reviser,
    DateTime? ReviseDate, decimal? DataStamp);
public sealed record Op105MemberDto(string OpGroupNo, string OpNo, string? OpName, int OpRoleFlag,
    decimal WeightRate, string? Creator, DateTime? CreateDate, string? Reviser, DateTime? ReviseDate,
    decimal? DataStamp);
public sealed record Op105MembersDto(Op105GroupDto Group, IReadOnlyList<Op105MemberDto> Items);
public sealed record Op105MemberSavedDto(Op105GroupDto Group, Op105MemberDto Item);
public sealed record Op105MemberDeletedDto(Op105GroupDto Group);
public sealed record Op105SegmentOption(string PsNo, string? PsName, int? IssueState, bool IsAll);
public sealed record Op105OperationOption(string OpNo, string OpName, string? PsNo, int IssueState);

public class Op105GroupWriteRequest
{
    public string? OpGroupNo { get; set; }
    public string? OpGroupName { get; set; }
    [JsonRequired] public string? PsNo { get; set; }
    [JsonRequired] public int? OpGroupOrder { get; set; }
    public string? Description { get; set; }
}

public sealed class Op105GroupUpdateRequest : Op105GroupWriteRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public sealed class Op105ExpectedRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public class Op105MemberFieldsRequest
{
    [JsonRequired] public int? OpRoleFlag { get; set; }
    [JsonRequired] public decimal? WeightRate { get; set; }
}

public sealed class Op105MemberCreateRequest : Op105MemberFieldsRequest
{
    public string? OpNo { get; set; }
}

public sealed class Op105MemberUpdateRequest : Op105MemberFieldsRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}
