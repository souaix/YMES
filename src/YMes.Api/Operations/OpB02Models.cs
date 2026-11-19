using System.Text.Json.Serialization;

namespace YMes.Api.Operations;

public sealed class OpB02RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed record OpB02GroupDto(string OpGroupNo, string OpGroupName, string? PsNo,
    int? OpGroupOrder, string? Description, int IssueState, string? Creator, DateTime? CreateDate,
    decimal? DataStamp, string? Reviser, DateTime? ReviseDate);
public sealed record OpB02OperationDto(string OpGroupNo, string OpNo, string? OpName);
public sealed record OpB02OperationsDto(OpB02GroupDto Group, IReadOnlyList<OpB02OperationDto> Items);
public sealed record OpB02OperationSavedDto(OpB02GroupDto Group, OpB02OperationDto Item);
public sealed record OpB02OperationDeletedDto(OpB02GroupDto Group);
public sealed record OpB02DeleteResult(bool Deleted, OpB02GroupDto? Item);
public sealed record OpB02SegmentOption(string PsNo, string? PsName, int? IssueState);
public sealed record OpB02OperationOption(string OpNo, string OpName, string? PsNo,
    string? OpType, int? OpClass, int IssueState);

public class OpB02GroupWriteRequest
{
    public string? OpGroupNo { get; set; }
    public string? OpGroupName { get; set; }
    [JsonRequired] public string? PsNo { get; set; }
    [JsonRequired] public int? OpGroupOrder { get; set; }
    public string? Description { get; set; }
}

public sealed class OpB02GroupUpdateRequest : OpB02GroupWriteRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}

public class OpB02ExpectedRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}

// Detail has only (OPGroupNo, OPNo). DataStamp is always the PARENT's CAS.
public sealed class OpB02OperationCreateRequest : OpB02ExpectedRequest
{
    public string? OpNo { get; set; }
}
