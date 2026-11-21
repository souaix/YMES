using System.Text.Json.Serialization;

namespace YMes.Api.Wip;

// Candidate only. Native STATUS/audit/serial are server-owned, never request fields.
public class Wp109FieldsRequest
{
    [JsonRequired] public int? SwrType { get; set; }
    [JsonRequired] public string? CustomerNo { get; set; }
    [JsonRequired] public string? OpNo { get; set; }
    [JsonRequired] public string? DeviceNo { get; set; }
    [JsonRequired] public DateTime? StartTime { get; set; }
    [JsonRequired] public DateTime? EndTime { get; set; }
    [JsonRequired] public string? CustomerLotNo { get; set; }
    [JsonRequired] public string? MoNo { get; set; }
    [JsonRequired] public string? SwrDesc { get; set; }
    [JsonRequired] public string[]? LotNos { get; set; }
}

// JSON metadata part for multipart requests. New/copy has no caller-supplied number.
public sealed class Wp109CreateRequest : Wp109FieldsRequest
{
    [JsonRequired] public string? AttachmentAction { get; set; } // none / replace
}

public sealed class Wp109EditRequest : Wp109FieldsRequest
{
    [JsonRequired] public string? Version { get; set; }
    [JsonRequired] public string? AttachmentAction { get; set; } // keep / clear / replace
}

public class Wp109ExpectedVersionRequest
{
    [JsonRequired] public string? Version { get; set; }
}

public sealed class Wp109VoidRequest : Wp109ExpectedVersionRequest
{
    [JsonRequired] public string? AbortReason { get; set; } // required, empty is valid
}

public sealed record Wp109VersionedItemResult(Wp109SwrDto Item,
    IReadOnlyList<Wp109DetailDto> Details, string Version);
public sealed record Wp109DeleteResult(string SwrNo, int DeletedCount, int DeletedDetails);
public sealed record Wp109VoidResult(string SwrNo, string Outcome,
    Wp109VersionedItemResult? Saved, int DeletedCount, int DeletedDetails);
public sealed record Wp109NumberPreview(string SwrNo, string BusinessTimeZone, bool Reserved);

internal sealed class Wp109WriteRuleException(int statusCode, string message) : Exception(message)
{
    internal int StatusCode { get; } = statusCode;
}

// Internal full native15 representation. Never return LegacyAttachmentPath.
// Blob=null means SQL NULL; Length=0 plus SHA256(empty) means a real empty bytea.
internal sealed record Wp109BlobDigest(long Length, string Sha256);
internal sealed record Wp109RawHeader(string SwrNo, int SwrType, int Status,
    string CustomerNo, string OpNo, string? DeviceNo, DateTime? StartTime,
    DateTime? EndTime, string? CustomerLotNo, string? MoNo, Wp109BlobDigest? Blob,
    string SwrDesc, string? Creator, DateTime? CreateDate, string? LegacyAttachmentPath);

// Root-configured clock policy. Missing/incorrect deployment setting must fail503.
public sealed record Wp109WritePolicy(string BusinessTimeZone);
