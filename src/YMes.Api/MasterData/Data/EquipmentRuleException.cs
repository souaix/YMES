namespace MES.Api.Data;

public class MasterDataRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class EquipmentRuleException(int statusCode, string message) : MasterDataRuleException(statusCode, message) { }
