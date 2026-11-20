namespace YMes.Api.Wip;

public sealed class Wp109Query
{
    public bool SwrTypeEnabled { get; init; }
    public int? SwrType { get; init; }
    public bool CustomerNoEnabled { get; init; }
    public string? CustomerNo { get; init; }
    public bool LotNoEnabled { get; init; }
    public string[]? LotNos { get; init; }
    public bool DeviceNoEnabled { get; init; }
    public string? DeviceNo { get; init; }
    public bool OpNoEnabled { get; init; }
    public string[]? OpNos { get; init; }
    public bool CustomerLotNoEnabled { get; init; }
    public string? CustomerLotNo { get; init; }
    public bool MoNoEnabled { get; init; }
    public string? MoNo { get; init; }
    public int Status { get; init; } = 0;
    public int Limit { get; init; } = 200;
}

// The native AttachFilePath is never exposed. Nullable length distinguishes a
// NULL BLOB from an actual zero-length BLOB; the source Y/N is path-derived.
public sealed record Wp109AttachmentDto(string SourceHaveAttachFile, long? BlobLength,
    bool DownloadAvailable);

// Native timestamp-without-time-zone values retain their wall-clock ticks and
// Kind=Unspecified. Neither scheduling dates nor legacy audit receive fake UTC.
public sealed record Wp109SwrDto(string SwrNo, int SwrType, int Status,
    string CustomerNo, string OpNo, string? DeviceNo, DateTime? StartTime,
    DateTime? EndTime, string? CustomerLotNo, string? MoNo, string SwrDesc,
    string? Creator, DateTime? CreateDate, Wp109AttachmentDto Attachment);

public sealed record Wp109DetailDto(string SwrNo, string LotNo);
public sealed record Wp109QueryRowDto(Wp109SwrDto Item, string? LotNo);
public sealed record Wp109QueryResult(IReadOnlyList<Wp109QueryRowDto> Items, int Limit, bool HasMore);
public sealed record Wp109ItemResult(Wp109SwrDto Item, IReadOnlyList<Wp109DetailDto> Details);

internal sealed record Wp109Download(long Length, string ContentType, string FileName);
internal sealed class Wp109ReadRuleException(int statusCode, string message) : Exception(message)
{
    internal int StatusCode { get; } = statusCode;
}
