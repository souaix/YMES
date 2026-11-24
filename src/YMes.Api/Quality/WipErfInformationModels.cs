namespace YMes.Api.Quality;

public sealed record WipErfInformationResponse(
    int ErfSource,
    IReadOnlyList<WipErfHoldCodeRow> CreateHoldCodes,
    IReadOnlyList<WipErfHoldCodeDispositionRow> HoldCodeDispositions,
    IReadOnlyList<WipErfDispositionRow> ErfDispositions,
    IReadOnlyList<WipErfLotDispositionRow> LotDispositions,
    IReadOnlyList<WipErfAttachmentRow> Attachments);

public sealed record WipErfHoldCodeRow(
    string ComponentNo, string AreaNo, string HoldItemNo, string? HoldItemName,
    decimal RecordQty, string? Description, string? OcapNo);

public sealed record WipErfHoldCodeDispositionRow(
    string ComponentNo, string AreaNo, string HoldItemNo, string? HoldItemName,
    decimal RecordQty, string? Description, string? HoldDisposition);

public sealed record WipErfDispositionRow(
    int DispOrder, string? GroupNo, string? DispDescription, string? ContactorName,
    string? NextDispType, string? NextGroupNo, string? Creator, DateTime? CreateDate,
    string? OcapNo, string? HoldItemNo, string? OcapNodeNo, string? DispUserNo, DateTime? DispDate);

public sealed record WipErfLotDispositionRow(
    int LotDispOrder, int LotDispType, string? Description, string? Creator,
    DateTime? CreateDate, string? OpNo, string? ProcessNo, string? ProcessVersion);

public sealed record WipErfAttachmentRow(
    string Source, string LotNo, string Name, string AttachType, string? Creator,
    DateTime? CreatedAt, string? OcapNo, string? OcapVersion, string? OcapNodeNo);

public sealed record WipErfAttachmentContent(byte[] Bytes, string FileName);

public sealed class WipErfInformationException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
