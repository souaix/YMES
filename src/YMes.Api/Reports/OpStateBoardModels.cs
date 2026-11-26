namespace YMes.Api.Reports;

public sealed record OpStateMetric(long LotCount, decimal CurrentQty);

public sealed record OpStateBoardItem(
    string OpNo,
    string OpName,
    decimal? OpOrder,
    string? PsNo,
    OpStateMetric Queue,
    OpStateMetric Run,
    OpStateMetric Hold,
    OpStateMetric Wait);

public sealed record OpStateBoardResponse(IReadOnlyList<OpStateBoardItem> Items);

public sealed record OpStateBreakdownItem(
    string OpNo,
    string GroupKey,
    OpStateMetric Queue,
    OpStateMetric Run,
    OpStateMetric Hold,
    OpStateMetric Wait);

public sealed record OpStateBreakdownResponse(
    string GroupBy,
    IReadOnlyList<OpStateBreakdownItem> Items);

public sealed record OpStateLotRow(
    string OpNo,
    string LotNo,
    string ProductNo,
    string ProductName,
    string ProductVersion,
    string ProductType,
    string BrNo,
    int Status,
    decimal CurQty,
    string CurUnitNo);

public sealed record OpStateLotsPage(
    int Page,
    int PageSize,
    bool HasMore,
    IReadOnlyList<OpStateLotRow> Items);

public sealed class OpStateRuleException(int statusCode, string message)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
