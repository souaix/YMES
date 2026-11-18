namespace YMes.Api.Quality;

public sealed record Qc501MachineTypeDto(
    string InspMachineType,
    string? InspMachineTypeName,
    int InspType,
    decimal? BaudRate,
    string? ParityBit,
    int? DataBit,
    int? StopBit,
    string? EndCode,
    string? Format1,
    string? Format2,
    string? ChNo,
    string? InspDriverNo,
    string? Description,
    string? Creator,
    DateTime? CreateDate,
    int IssueState,
    decimal? DataStamp,
    string? Reviser,
    DateTime? ReviseDate);

public sealed record Qc502InspectionMachineDto(
    string InspMachineNo,
    string? InspMachineName,
    string? InspMachineType,
    int InspType,
    decimal? BaudRate,
    string? ParityBit,
    int? DataBit,
    int? StopBit,
    string? EndCode,
    string? Format1,
    string? Format2,
    string? ChNo,
    string? InspDriverNo,
    string? Description,
    string? Creator,
    DateTime? CreateDate,
    int IssueState,
    decimal? DataStamp,
    string? Reviser,
    DateTime? ReviseDate);

public sealed record QcInspectionDriverDto(string InspDriverNo, string? FunctionName, string? Description);

public sealed class Qc501MachineTypeWriteRequest
{
    public string InspMachineType { get; set; } = "";
    public string? InspMachineTypeName { get; set; }
    public int InspType { get; set; }
    public decimal? BaudRate { get; set; }
    public string? ParityBit { get; set; }
    public int? DataBit { get; set; }
    public int? StopBit { get; set; }
    public string? EndCode { get; set; }
    public string? Format1 { get; set; }
    public string? Format2 { get; set; }
    public string? ChNo { get; set; }
    public string? InspDriverNo { get; set; }
    public string? Description { get; set; }
    public int IssueState { get; set; }
    public decimal? DataStamp { get; set; }
}

public sealed class Qc502InspectionMachineWriteRequest
{
    public string InspMachineNo { get; set; } = "";
    public string? InspMachineName { get; set; }
    public string? InspMachineType { get; set; }
    public int InspType { get; set; }
    public decimal? BaudRate { get; set; }
    public string? ParityBit { get; set; }
    public int? DataBit { get; set; }
    public int? StopBit { get; set; }
    public string? EndCode { get; set; }
    public string? Format1 { get; set; }
    public string? Format2 { get; set; }
    public string? ChNo { get; set; }
    public string? InspDriverNo { get; set; }
    public string? Description { get; set; }
    public int IssueState { get; set; }
    public decimal? DataStamp { get; set; }
}

public sealed class QcInspectionStateRequest
{
    public int IssueState { get; set; }
    public decimal? DataStamp { get; set; }
    // Legacy web service contracts include Creator. The API always uses the
    // authenticated principal for audit fields instead of trusting this value.
    public string? Creator { get; set; }
}

public sealed class QcInspectionRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
