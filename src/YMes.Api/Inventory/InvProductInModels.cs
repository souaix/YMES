namespace YMes.Api.Inventory;

public sealed class InvProductInRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed record InvProductInListItem(
    int ProductInType,
    string ProductInNo,
    int State,
    string? Description,
    string? Creator,
    DateTime CreateDate,
    string InventoryNo,
    DateTime? InputDate,
    int Source,
    string? FromInventoryNo,
    int? ToErp,
    decimal? DataStamp);

public sealed record InvProductInPage(
    IReadOnlyList<InvProductInListItem> Items,
    long TotalCount,
    int Page,
    int PageSize);

public sealed record InvProductInDetailRow(
    string ProductNo,
    string ProductVersion,
    string LotNo,
    decimal Qty,
    string UnitNo,
    string LocatorNo,
    string BaseLotNo,
    DateTime? InputDate,
    string? FromInventoryNo);

public sealed record InvProductInComponentRow(
    string LotNo,
    string ComponentNo,
    decimal? GoodQty,
    decimal? ScrapQty,
    string? UnitNo);

public sealed record InvProductInDetails(
    InvProductInListItem Basis,
    IReadOnlyList<InvProductInDetailRow> Details,
    IReadOnlyList<InvProductInComponentRow> Components);
