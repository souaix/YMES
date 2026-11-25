namespace YMes.Api.Inventory;

public sealed class InvFgdInventoryRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed record InvFgdInventoryBasis(string InventoryNo, string? InventoryName);

public sealed record InvFgdInventoryBasisPage(
    IReadOnlyList<InvFgdInventoryBasis> Items,
    long TotalCount,
    int Page,
    int PageSize);

public sealed record InvFgdInventoryRow(
    string InventoryNo,
    string ProductNo,
    string LocatorNo,
    string LotNo,
    decimal LocQty,
    string? UnitNo,
    int BelongToType,
    string? BelongToNo,
    DateTime? InputDate,
    string? FgdInNo);

public sealed record InvFgdInventoryPage(
    IReadOnlyList<InvFgdInventoryRow> Items,
    long TotalCount,
    int Page,
    int PageSize);

public sealed record InvFgdInventoryComponent(
    string ComponentNo,
    decimal? GoodQty,
    decimal? ScrapQty,
    string? UnitNo);

public sealed record InvFgdBinDefinition(string BinNo, int BinOrder);

public sealed record InvFgdBinQuantity(string SourceColumn, decimal? Quantity);

public sealed record InvFgdGoodBinState(
    string LotNo,
    string? LotSerial,
    IReadOnlyList<InvFgdBinQuantity> Bins);

public sealed record InvFgdInventoryDetails(
    IReadOnlyList<InvFgdInventoryComponent> Components,
    IReadOnlyList<InvFgdBinDefinition> BinDefinitions,
    InvFgdGoodBinState? GoodBinState);

public sealed record InvFgdInConfirmation(
    string FgdInNo,
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

public sealed record InvFgdInPage(
    IReadOnlyList<InvFgdInConfirmation> Items,
    long TotalCount,
    int Page,
    int PageSize);

public sealed record InvFgdInDetail(
    string ProductNo,
    string ProductVersion,
    string LotNo,
    decimal Qty,
    string UnitNo,
    string LocatorNo,
    string BaseLotNo,
    DateTime? InputDate,
    string? FromInventoryNo);

public sealed record InvFgdInComponent(
    string LotNo,
    string ComponentNo,
    decimal? GoodQty,
    decimal? ScrapQty,
    string? UnitNo);

public sealed record InvFgdInConfirmationDetails(
    InvFgdInConfirmation Basis,
    IReadOnlyList<InvFgdInDetail> Details,
    IReadOnlyList<InvFgdInComponent> Components);
