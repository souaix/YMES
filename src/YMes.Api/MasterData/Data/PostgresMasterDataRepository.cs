using System.Globalization;
using System.Security.Claims;
using MES.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace MES.Api.Data;

public sealed partial class PostgresMasterDataRepository : IMasterDataRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IOperationRepository _operations;

    public PostgresMasterDataRepository(NpgsqlDataSource dataSource, IOperationRepository operations)
    {
        _dataSource = dataSource;
        _operations = operations;
    }

    public IReadOnlyList<OperationDto> ListOperations(string? keyword, int? issueState) => _operations.List(keyword, issueState);
    public OperationDto GetOperation(string no) => _operations.Get(no);
    public OperationDto CreateOperation(OperationWriteRequest request, string actorUserNo) => _operations.Create(request, actorUserNo);
    public OperationDto UpdateOperation(string no, OperationWriteRequest request, string actorUserNo) => _operations.Update(no, request, actorUserNo);
    public OperationDto ApproveOperation(string no, int dataStamp, string actorUserNo) => _operations.Approve(no, dataStamp, actorUserNo);
    public void DeleteOperation(string no, int issueState, int dataStamp, string actorUserNo) => _operations.DeleteOrRetire(no, issueState, dataStamp, actorUserNo);

    private static void AddVarchar(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static void AddNumeric(NpgsqlCommand command, string name, object? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value is null ? DBNull.Value : Convert.ToDecimal(value, CultureInfo.InvariantCulture);

    private static void AddBoolean(NpgsqlCommand command, string name, bool value) =>
        command.Parameters.Add(name, NpgsqlDbType.Boolean).Value = value;

    private static void AddUuid(NpgsqlCommand command, string name, Guid value) =>
        command.Parameters.Add(name, NpgsqlDbType.Uuid).Value = value;

    private static int ToInt32(object value) => Convert.ToInt32(value, CultureInfo.InvariantCulture);
    private static long ToInt64(object value) => Convert.ToInt64(value, CultureInfo.InvariantCulture);

    private static string Required(string? value, string field, int maxLength)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0) throw BadRequest($"{field}不可空白。");
        if (text.Length > maxLength) throw BadRequest($"{field}不可超過 {maxLength} 個字元。");
        return text;
    }

    private static string RequiredCode(string? value, string field, int maxLength) => NormalizeCode(Required(value, field, maxLength));

    private static string Optional(string? value, int maxLength, string field = "欄位")
    {
        var text = value?.Trim() ?? "";
        if (text.Length > maxLength) throw BadRequest($"{field}不可超過 {maxLength} 個字元。");
        return text;
    }

    private static string NormalizeCode(string? value) => (value ?? "").Trim().ToUpperInvariant();

    private static bool Same(string? left, string? right) =>
        string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static void ValidateActor(string actorUserNo)
    {
        if (string.IsNullOrWhiteSpace(actorUserNo) || actorUserNo.Trim().Length > 10)
            throw BadRequest("登入使用者編號長度不符合作業資料欄位。");
    }

    private static void EnsureStamp(int current, int expected)
    {
        if (current != expected) throw Conflict("資料已被其他人更新，請重新載入後再操作。");
    }

    private static void EnsureEditable(int issueState, int dataStamp, int expectedStamp)
    {
        if (issueState != 0) throw Conflict("只有 UNFROZEN 主檔可以編輯。");
        EnsureStamp(dataStamp, expectedStamp);
    }

    private static void EnsureRemoval(int issueState, int requestedState, int dataStamp, int expectedStamp)
    {
        if (issueState != requestedState) throw Conflict("主檔狀態已變更，請重新載入後再操作。");
        if (issueState == 1) throw Conflict("PENDING 主檔不可刪除。");
        EnsureStamp(dataStamp, expectedStamp);
    }

    private static MasterDataRuleException NotFound(string entity, string id) => new(404, $"找不到{entity} {id}。");
    private static MasterDataRuleException Conflict(string message) => new(409, message);
    private static MasterDataRuleException BadRequest(string message) => new(400, message);

    private static void ExecuteInsert(NpgsqlCommand command, string duplicateMessage)
    {
        try { command.ExecuteNonQuery(); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw Conflict(duplicateMessage);
        }
    }

    private static void ExecuteOptimisticUpdate(NpgsqlCommand command, string message)
    {
        if (command.ExecuteNonQuery() == 0) throw Conflict(message);
    }
}
