namespace YMes.Api.Wip;

public sealed class Wp914LotQuery
{
    public string? RoNo { get; set; }
    public string? MoNo { get; set; }
    public string? ProductNo { get; set; }
    public string? AreaNo { get; set; }
    public string? OpNo { get; set; }
    public string? LotNo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed class Wp914LookupQuery
{
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed record Wp914Page<T>(int Page, int PageSize, bool HasMore, IReadOnlyList<T> Items);

public sealed record Wp914LotItem(
    string LotNo,
    string BaseLotNo,
    string RoNo,
    string AreaNo,
    string MoNo,
    string OpNo,
    string ProductNo,
    string PsNo,
    string ProcessNo,
    string OpName,
    string BrNo,
    string OpGroupNo,
    decimal CurQty,
    string CurUnitNo,
    decimal SysQty,
    string SysUnitNo,
    int Status,
    int LotState,
    int HotLot,
    int Priority,
    decimal Version);

public sealed class Wp914SetRequest
{
    public int? HotLot { get; set; }
    public int? Priority { get; set; }
    public IReadOnlyList<Wp914SelectedLot?>? Lots { get; set; }
}

public sealed class Wp914SelectedLot
{
    public string? LotNo { get; set; }
    public string? BaseLotNo { get; set; }
    public string? OpNo { get; set; }
    public int? Status { get; set; }
    public decimal? Version { get; set; }
}

public sealed record Wp914BaseUpdate(
    string BaseLotNo,
    IReadOnlyList<string> SelectedLotNos,
    long AffectedLotCount,
    int HotLot,
    int Priority);

public sealed record Wp914SetResult(
    Guid RequestId,
    DateTime UpdatedAtUtc,
    string UpdatedBy,
    IReadOnlyList<Wp914BaseUpdate> Updates,
    bool Replayed = false);

public sealed class Wp914RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
