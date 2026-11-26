namespace YMes.Api.Inventory;

public sealed record InvProductInLogRow(
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
    string? Reviser,
    DateTime? ReviseDate,
    int? ToErp,
    string? ErpNo,
    string? CancelUser,
    DateTime? CancelDate,
    decimal? DataStamp);

public sealed record InvProductInLogPage(
    IReadOnlyList<InvProductInLogRow> Items,
    long TotalCount,
    int Page,
    int PageSize);

public sealed record InvProductInLogDetailRow(
    string ProductNo,
    string? ProductName,
    string ProductVersion,
    string LotNo,
    decimal Qty,
    string UnitNo,
    string LocatorNo,
    string BaseLotNo,
    DateTime? InputDate,
    string? FromInventoryNo);

public sealed record InvProductInLogComponentRow(
    string LotNo,
    string ComponentNo,
    decimal? GoodQty,
    decimal? ScrapQty,
    string? UnitNo);

public sealed record InvProductInLogDetails(
    InvProductInLogRow Basis,
    IReadOnlyList<InvProductInLogDetailRow> Details,
    IReadOnlyList<InvProductInLogComponentRow> Components);
