using System.ComponentModel.DataAnnotations;

namespace MES.Contracts;

public sealed record ProcessDto
{
    public required string ProcessNo { get; init; }
    public required string ProcessVersion { get; init; }
    public required string ProcessType { get; init; }
    public required string ProductionSegmentNo { get; init; }
    public int ProcessClass { get; init; }
    public string Description { get; init; } = "";
    public bool TuningProcess { get; init; }
    public int PartialType { get; init; }
    public int IssueState { get; init; }
    public bool CurrentVersion { get; init; }
    public int DataStamp { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class ProcessWriteRequest
{
    [Required, StringLength(40)] public string ProcessNo { get; set; } = "";
    [Required, StringLength(10)] public string ProcessVersion { get; set; } = "01";
    [Required, StringLength(40)] public string ProcessType { get; set; } = "";
    [Required, StringLength(40)] public string ProductionSegmentNo { get; set; } = "";
    [Range(0, 9)] public int ProcessClass { get; set; }
    [StringLength(1000)] public string Description { get; set; } = "";
    public bool TuningProcess { get; set; }
    public int PartialType { get; set; }
    public int DataStamp { get; set; }
}

public sealed record ProcessNodeDto(string NodeId, string NodeNo, string NodeType, int Sequence, string OperationNo, string Description);
public sealed record ProcessEdgeDto(string FromNodeId, string ToNodeId, string Action);
public sealed record ProcessGraphDto(IReadOnlyList<ProcessNodeDto> Nodes, IReadOnlyList<ProcessEdgeDto> Edges, int DataStamp);
