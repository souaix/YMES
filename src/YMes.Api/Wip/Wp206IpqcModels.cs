namespace YMes.Api.Wip;

public sealed class Wp206OperationQuery
{
    public string? Keyword { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed class Wp206RecordRequest
{
    public string? LotNo { get; set; }
    public string? OpNo { get; set; }
    public IReadOnlyList<Wp206AbnormalSelection?>? Abnormalities { get; set; }
}

public sealed class Wp206AbnormalSelection
{
    public string? AbnormalNo { get; set; }
    public IReadOnlyList<string?>? Positions { get; set; }
}

public sealed record Wp206OperationItem(string OpNo, string OpName);

public sealed record Wp206AbnormalItem(
    string AbnormalNo,
    string AbnormalName,
    string AbnormalType,
    string LinkName,
    decimal? Priority,
    IReadOnlyList<string> Positions);

public sealed record Wp206Page<T>(int Page, int PageSize, bool HasMore, IReadOnlyList<T> Items);

public sealed record Wp206RecordResult(int InsertedCount, bool Replayed);

public sealed class Wp206IpqcRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
