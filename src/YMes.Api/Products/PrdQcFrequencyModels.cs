using System.Text.Json.Serialization;

namespace YMes.Api.Products;

public sealed record PrdQcFrequencyDto(string ProductNo, string EquipmentNo, string OpNo, int QcMethod,
    int? QcNum, string? ShiftA, string? ShiftB, string? Description, string? Creator, DateTime? CreateDate,
    string? QcItem, int? QcType, string? ModuleNo, decimal? DataStamp, string? Reviser, DateTime? ReviseDate);
public sealed record PrdQcFrequencyOption(string Value, string? Label);

public class PrdQcFrequencyKeyRequest
{
    [JsonRequired] public string? ProductNo { get; set; }
    [JsonRequired] public string? EquipmentNo { get; set; }
    [JsonRequired] public string? OpNo { get; set; }
}
public class PrdQcFrequencyWriteRequest : PrdQcFrequencyKeyRequest
{
    [JsonRequired] public int? QcMethod { get; set; }
    public int? QcNum { get; set; }
    public string? ShiftA { get; set; }
    public string? ShiftB { get; set; }
    public string? Description { get; set; }
}
public sealed class PrdQcFrequencyUpdateRequest : PrdQcFrequencyWriteRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}
public sealed class PrdQcFrequencyDeleteRequest : PrdQcFrequencyKeyRequest
{
    [JsonRequired] public decimal? DataStamp { get; set; }
}
public sealed class PrdQcFrequencyRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
