namespace YMes.Api.Reports;

public sealed record ConfirmLotRow(
    string ProductNo,
    string ProductName,
    string ProductVersion,
    string LotNo,
    string OpNo,
    string BrNo,
    int Status,
    decimal CurQty,
    string CurUnitNo);

public sealed record ConfirmLotPage(
    int Page,
    int PageSize,
    bool HasMore,
    IReadOnlyList<ConfirmLotRow> Items);

public sealed class ConfirmLotRuleException(int statusCode, string message)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
