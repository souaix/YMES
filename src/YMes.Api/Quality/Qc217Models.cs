namespace YMes.Api.Quality;

public sealed record Qc217InspectionDto(
    string OpNo,
    string EquipmentNo,
    int EqpDataType,
    int InspType,
    int InspWeek,
    string InspStartTime,
    int? PeriodMin,
    int? EquipmentState,
    int? LotCount,
    int IssueState,
    string MailGroupNo,
    string CcMailGroupNo,
    string Creator,
    DateTime? CreateDate,
    string Reviser,
    DateTime? ReviseDate,
    decimal DataStamp);

public sealed record Qc217InspectionDetailDto(
    string QcItemNo,
    string QcItemName,
    int QcItemType,
    int LotIsValid);

public sealed record Qc217ValueOption(int Value, string Label);

public sealed record Qc217OptionsDto(
    IReadOnlyList<string> Operations,
    IReadOnlyList<string> Equipment,
    IReadOnlyList<string> EquipmentTypes,
    IReadOnlyList<Qc217ValueOption> EquipmentStates,
    IReadOnlyList<Qc217ValueOption> InspectionWeeks,
    IReadOnlyList<string> MailGroups,
    IReadOnlyList<Qc217InspectionDetailDto> QcItems,
    IReadOnlyList<string> MissingReferences);

public sealed class Qc217InspectionWriteRequest
{
    public string OpNo { get; set; } = "";
    public string EquipmentNo { get; set; } = "";
    public int EqpDataType { get; set; }
    public int InspType { get; set; }
    public int InspWeek { get; set; } = -1;
    public string InspStartTime { get; set; } = "N/A";
    public int? PeriodMin { get; set; }
    public int? EquipmentState { get; set; }
    public int? LotCount { get; set; }
    public string? MailGroupNo { get; set; }
    public string? CcMailGroupNo { get; set; }
    public decimal DataStamp { get; set; }
    public List<Qc217InspectionDetailRequest> Details { get; set; } = [];
}

public sealed class Qc217InspectionDetailRequest
{
    public string QcItemNo { get; set; } = "";
    public int QcItemType { get; set; }
}

public sealed class Qc217StampRequest
{
    public decimal DataStamp { get; set; }
}

public sealed class Qc217RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
