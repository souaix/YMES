using MES.Api.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Processes;

// ProcessNo and ProcessType share an identifier namespace in the MES source.
// All cooperating creators acquire this mutex before any parent/child row locks.
public static class PrsIdentifierGuard
{
    public static void LockIdentifier(NpgsqlConnection connection, NpgsqlTransaction transaction, string identifier)
    {
        var value = (identifier ?? "").Trim();
        if (value.Length is < 1 or > 30 || value.Any(char.IsControl)) throw Rule(400, "流程識別碼必填、最多 30 字元，不可含控制字元。");
        using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:PRS:IDENTIFIER:' || lower(@no),0));", connection, transaction);
        Text(command, "no", value, 30); command.ExecuteNonQuery();
    }

    public static void EnsureTypeIdentifierAvailable(NpgsqlConnection connection, NpgsqlTransaction transaction, string processType)
    {
        LockIdentifier(connection, transaction, processType);
        if (Exists(connection, transaction, "tblprsprocessbasis"))
            RejectExisting(connection, transaction, "tblprsprocessbasis", "processno", processType, "Process Type 不可與既有 ProcessNo 相同（包含全部流程狀態／版本）。");
    }

    public static void EnsureProcessIdentifierAvailable(NpgsqlConnection connection, NpgsqlTransaction transaction, string processNo)
    {
        LockIdentifier(connection, transaction, processNo);
        if (Exists(connection, transaction, "tblprstype"))
            RejectExisting(connection, transaction, "tblprstype", "processtype", processNo, "ProcessNo 不可與既有 Process Type 相同（包含全部類別狀態）。");
    }

    public static string LockApprovedType(NpgsqlConnection connection, NpgsqlTransaction transaction, string processType)
    {
        var value = (processType ?? "").Trim();
        if (value.Length is < 1 or > 20 || value.Any(char.IsControl)) throw Rule(400, "新流程必須選擇有效的 Process Type，最多 20 字元。");
        if (!Exists(connection, transaction, "tblprstype")) throw Rule(503, "尚未建立流程類別主檔，無法驗證新流程的已核准 Process Type。");
        using var command = new NpgsqlCommand("SELECT processtype,issuestate::int FROM \"YMES\".tblprstype WHERE lower(processtype)=lower(@type) ORDER BY processtype FOR SHARE;", connection, transaction);
        Text(command, "type", value, 20);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(422, "Process Type 不存在，請重新選擇已核准流程類別。");
        var stored = reader.GetString(0);
        var state = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
        if (reader.Read()) throw Rule(409, "Process Type 存在大小寫不同的重複資料，請先整理。");
        if (state != 2) throw Rule(422, "新流程只能使用已核准（狀態 2）的 Process Type。");
        return stored;
    }

    private static void RejectExisting(NpgsqlConnection connection, NpgsqlTransaction transaction, string table, string column, string no, string message)
    {
        // table/column are fixed internal allowlist values, never request fields.
        using var command = new NpgsqlCommand($"SELECT 1 FROM \"YMES\".{table} WHERE lower({column})=lower(@no) LIMIT 1;", connection, transaction);
        Text(command, "no", no, 30);
        if (command.ExecuteScalar() is not null) throw Rule(409, message);
    }

    internal static bool Exists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@table);", connection, transaction);
        Text(command, "table", table, 63); return command.ExecuteScalar() is true;
    }
    private static void Text(NpgsqlCommand command, string name, string value, int size) => command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = value;
    private static MasterDataRuleException Rule(int status, string message) => new(status, message);
}
