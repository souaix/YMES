namespace YMes.Api.Wip;

public sealed class Wp507FutureWaitQuery
{
    public string? LotNo { get; set; }
    public string? OpNo { get; set; }
    public int? WaitTime { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed class Wp507OpLookupQuery
{
    public string? Keyword { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record Wp507FutureWaitRow(
    string LotNo,
    string OpNo,
    int WaitTime,
    string? Description,
    string? Creator,
    DateTime? CreateDate);

public sealed record Wp507FutureWaitPage(
    int Page,
    int PageSize,
    bool HasMore,
    IReadOnlyList<Wp507FutureWaitRow> Items);

public sealed record Wp507FutureWaitPageOf<T>(int Page, int PageSize, bool HasMore, IReadOnlyList<T> Items);

public sealed record Wp507OpOption(string OpNo, string OpName);

public sealed class Wp507FutureWaitRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
