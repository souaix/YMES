namespace YMes.Api.Wip;

public sealed class Wp510WaitBasisQuery
{
    public string? LotNo { get; set; }
    public string? WaitNo { get; set; }
    public string? OpNo { get; set; }
    public string? CustomerNo { get; set; }
    public string? CreateFrom { get; set; }
    public string? CreateTo { get; set; }
    public string? ReleaseFrom { get; set; }
    public string? ReleaseTo { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed class Wp510LookupQuery
{
    public string? Keyword { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record Wp510Page<T>(int Page, int PageSize, bool HasMore, IReadOnlyList<T> Items);

public sealed record Wp510WaitRow(
    string WaitNo,
    string LotNo,
    string? ProductNo,
    string LotSerial,
    int Status,
    string OpNo,
    string? CustomerNo,
    string? WaitDescription,
    string? RuleNo,
    decimal CreateQty,
    string Creator,
    DateTime CreateDate,
    decimal? ReleaseQty,
    string? Releaser,
    DateTime? ReleaseDate);

public sealed record Wp510DispositionRow(
    string WaitNo,
    int LotDispType,
    string? Description,
    string Creator,
    DateTime CreateDate,
    string? OpNo,
    string? ProcessNo,
    string? ProcessVersion);

public sealed class Wp510RuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
