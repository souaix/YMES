namespace YMes.Api.Wip;

public sealed class Wp307FilterOptionsQuery
{
    public string? DepartmentNo { get; set; }
}

public sealed class Wp307CurrentQuery
{
    public string? DepartmentNo { get; set; }
    public string? ShiftNo { get; set; }
    public string? UserNo { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed class Wp307HistoryQuery
{
    public string? DepartmentNo { get; set; }
    public string? ShiftNo { get; set; }
    public string? UserNo { get; set; }
    public DateTimeOffset? LoginStartDate { get; set; }
    public DateTimeOffset? LoginEndDate { get; set; }
    public DateTimeOffset? LogoutStartDate { get; set; }
    public DateTimeOffset? LogoutEndDate { get; set; }
    public decimal? ExceptionTime { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record Wp307FilterOptionsResponse(
    string AreaNo,
    string DepartmentNo,
    IReadOnlyList<Wp307ShiftOption> Shifts,
    IReadOnlyList<Wp307UserOption> Users);

public sealed record Wp307ShiftOption(
    string Value,
    string Label,
    string ShiftNo,
    string ShiftName,
    DateTime? FromTime,
    DateTime? ToTime);

public sealed record Wp307UserOption(string UserNo, string UserName);

public sealed record Wp307OperatorEligibleResponse(
    bool Eligible,
    string DepartmentNo,
    IReadOnlyList<string> ShiftNos,
    string? AlreadyLoggedInAreaNo = null,
    string? ShiftNo = null);

public sealed record Wp307OperatorStateRow(
    string AreaNo,
    string DepartmentNo,
    string ShiftNo,
    string UserNo,
    string? LoginComment,
    DateTime LoginTime);

public sealed record Wp307OperatorHistoryRow(
    string AreaNo,
    string DepartmentNo,
    string ShiftNo,
    string UserNo,
    DateTime LoginTime,
    DateTime LogoutTime,
    decimal ExceptionTime,
    string? LoginComment,
    string? LogoutComment);

public sealed record Wp307Page<T>(int Page, int PageSize, bool HasMore, IReadOnlyList<T> Items);
