using MES.Api.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Operations;

public static class Op103IdentifierGuard
{
    public static void LockIdentifier(NpgsqlConnection connection, NpgsqlTransaction transaction, string identifier)
    {
        var key = Key(identifier);
        using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:OP:IDENTIFIER:' || lower(btrim(@no)),0));", connection, transaction);
        Text(command, "no", key, 20); command.ExecuteNonQuery();
    }

    public static void EnsureNewOperation(NpgsqlConnection connection, NpgsqlTransaction transaction, string opNo)
    {
        var key = Key(opNo);
        LockIdentifier(connection, transaction, key);
        RejectExisting(connection, transaction, "tblopbasis", "opno", key, "作業站編號已存在（不分大小寫／前後空白）。");
        // Before OP103 deployment the catalog may genuinely be absent. Once
        // present, insufficient privilege must fail closed, never mean empty.
        if (Exists(connection, transaction, "tbloptype"))
            RejectExisting(connection, transaction, "tbloptype", "optype", key, "作業站編號不可與既有作業站類別同名。");
    }

    public static void EnsureNewType(NpgsqlConnection connection, NpgsqlTransaction transaction, string opType)
    {
        var key = Key(opType);
        LockIdentifier(connection, transaction, key);
        RejectExisting(connection, transaction, "tbloptype", "optype", key, "作業站類別已存在。");
        RejectExisting(connection, transaction, "tblopbasis", "opno", key, "作業站類別不可與任何狀態的既有作業站編號同名。");
    }

    public static string LockExistingType(NpgsqlConnection connection, NpgsqlTransaction transaction, string? requested)
    {
        var key = Key(requested);
        using var command = new NpgsqlCommand("SELECT optype FROM \"YMES\".tbloptype WHERE lower(btrim(optype))=lower(@no) ORDER BY optype FOR SHARE;", connection, transaction);
        Text(command, "no", key, 20);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(422, "找不到指定作業站類別，請重新選擇。");
        var actual = reader.GetString(0);
        if (reader.Read()) throw Rule(409, "作業站類別存在大小寫或空白歧義，請先整理主檔。");
        // Source has not established an approved-only OP Type selector.
        return actual;
    }

    public static string? ResolveForUpdate(NpgsqlConnection connection, NpgsqlTransaction transaction, string? requested, string? rawExisting)
    {
        if (string.Equals(requested?.Trim() ?? "", rawExisting?.Trim() ?? "", StringComparison.OrdinalIgnoreCase)) return rawExisting;
        return LockExistingType(connection, transaction, requested);
    }

    internal static bool Exists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@table AND c.relkind IN ('r','p'));", connection, transaction);
        Text(command, "table", table, 63); return command.ExecuteScalar() is true;
    }
    internal static string Key(string? value)
    {
        var text = value?.Trim() ?? "";
        if (text.Length is < 1 or > 20 || text.Any(char.IsControl)) throw Rule(400, "作業站／類別編號必填，最多20字，不可含控制字元。");
        return text;
    }
    private static void RejectExisting(NpgsqlConnection connection, NpgsqlTransaction transaction, string table, string column, string key, string message)
    {
        // Only fixed internal table/column identifiers are accepted by callers.
        using var command = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM \"YMES\".{table} WHERE lower(btrim({column}))=lower(@no));", connection, transaction);
        Text(command, "no", key, 20);
        if (command.ExecuteScalar() is true) throw Rule(409, message);
    }
    internal static void Text(NpgsqlCommand command, string name, string? value, int size) => command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = (object?)value ?? DBNull.Value;
    internal static MasterDataRuleException Rule(int status, string message) => new(status, message);
}
