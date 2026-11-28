namespace YMes.Api.Wip;

public sealed class Wp303EquipmentConsumeStatusQuery
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record Wp303EquipmentConsumeStatusItem(
    string EquipmentNo,
    string ConsumeNo,
    string ConsumeSeq,
    decimal CurQty,
    decimal CurPeriod,
    string? Creator,
    DateTime? CreateDate,
    string? Revisor,
    DateTime? ReviseDate);

public sealed record Wp303EquipmentConsumeStatusPage<T>(
    int Page,
    int PageSize,
    bool HasMore,
    IReadOnlyList<T> Items,
    long? TotalCount = null);

public class Wp303EquipmentConsumeStatusLookupQuery
{
    public string? Keyword { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed class Wp303EquipmentConsumeBasisLookupQuery : Wp303EquipmentConsumeStatusLookupQuery
{
    public string? EquipmentNo { get; set; }
}

public sealed record Wp303EquipmentConsumeBasisItem(
    string EquipmentType,
    string ConsumeNo,
    decimal Quantity,
    decimal ToleranceQty,
    decimal Period,
    decimal TolerancePeriod);

public class Wp303EquipmentConsumeStatusAddRequest
{
    public string? EquipmentNo { get; set; }
    public string? ConsumeNo { get; set; }
    public string? ConsumeSeq { get; set; }
    public decimal? CurQty { get; set; }
    public decimal? CurPeriod { get; set; }
}

public sealed class Wp303EquipmentConsumeStatusEditRequest : Wp303EquipmentConsumeStatusAddRequest
{
    public string? OriginalConsumeSeq { get; set; }
    public decimal? OriginalCurQty { get; set; }
    public decimal? OriginalCurPeriod { get; set; }
}

public sealed class Wp303EquipmentConsumeStatusDeleteRequest
{
    public IReadOnlyList<Wp303EquipmentConsumeStatusExpectedItem>? Items { get; set; }
}

public class Wp303EquipmentConsumeStatusExpectedItem
{
    public string? EquipmentNo { get; set; }
    public string? ConsumeNo { get; set; }
    public string? ConsumeSeq { get; set; }
    public decimal? CurQty { get; set; }
    public decimal? CurPeriod { get; set; }
}

public sealed class Wp303EquipmentConsumeStatusResetRequest : Wp303EquipmentConsumeStatusExpectedItem
{
    public string? NewConsumeSeq { get; set; }
}

public sealed record Wp303EquipmentConsumeStatusDeleteResult(int DeletedCount);
