using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

// YMES integrity extension: attribute and variable items share child-table
// identifiers. All cooperating writers must acquire this namespace before
// locking any item rows. Future variable writers must use the same protocol;
// this helper alone does not protect writers that have not adopted it.
internal static class QcItemIdentityGuard
{
    private static readonly string[] ItemTables = ["tblqcitemforattributes", "tblqcitemforvariables"];

    internal static void Acquire(NpgsqlConnection connection, NpgsqlTransaction transaction, IEnumerable<string> nos)
    {
        if (nos is null) throw Rule(400, "QC Item 識別碼清單不可為 NULL。");
        var requested = nos.Select(Key).ToArray();
        var canonical = new List<string>();
        // PostgreSQL supplies both case folding and a consistent lock order.
        // Read the sorted keys before issuing locks so query evaluation order
        // cannot reorder the volatile advisory-lock calls.
        using (var command = new NpgsqlCommand("SELECT DISTINCT lower(btrim(value)) COLLATE \"C\" AS no FROM unnest(@nos) AS input(value) ORDER BY no;", connection, transaction))
        {
            command.Parameters.Add("nos", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = requested;
            using var reader = command.ExecuteReader();
            while (reader.Read()) canonical.Add(reader.GetString(0));
        }
        using var acquire = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:QCItem:Identifier:' || @no,0));", connection, transaction);
        var parameter = acquire.Parameters.Add("no", NpgsqlDbType.Text);
        foreach (var no in canonical)
        {
            parameter.Value = no;
            acquire.ExecuteNonQuery();
        }
    }

    internal static void RequireTables(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        // Catalog visibility establishes existence only. Later reads must
        // still succeed with the caller's privileges; errors never mean empty.
        using var command = new NpgsqlCommand("SELECT count(*) FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=ANY(@tables) AND c.relkind IN ('r','p');", connection, transaction);
        command.Parameters.Add("tables", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = ItemTables;
        if (command.ExecuteScalar() is not long count || count != ItemTables.Length)
            throw Rule(503, "計數／計量 QC Item 主檔尚未完整建立，無法驗證共用識別碼。");
    }

    // Call after Acquire, before any item row locks or child mutations.
    internal static void EnsureAvailable(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        var key = Key(no);
        RequireTables(connection, transaction);
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblqcitemforattributes WHERE lower(btrim(qcitemno))=lower(btrim(@no)) UNION ALL SELECT 1 FROM \"YMES\".tblqcitemforvariables WHERE lower(btrim(qcitemno))=lower(btrim(@no)));", connection, transaction);
        Text(command, key);
        if (command.ExecuteScalar() is true)
            throw Rule(409, "QC Item 編號已被計數或計量項目使用（不分大小寫／前後空白）。");
    }

    // YMES integrity extension: refuse an ambiguous shared-child owner. The
    // caller separately reads the canonical attribute item and rejects its
    // duplicates. This ordinary MVCC read never reverse-locks a variable row.
    internal static void EnsureAttributeOwner(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        var key = Key(no);
        RequireTables(connection, transaction);
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblqcitemforvariables WHERE lower(btrim(qcitemno))=lower(btrim(@no)));", connection, transaction);
        Text(command, key);
        if (command.ExecuteScalar() is true)
            throw Rule(409, "同名計量 QC Item 造成共用子資料歸屬不明，計數項目資料已保留。");
    }

    private static string Key(string? value)
    {
        // Match PostgreSQL btrim's default space trimming. Other whitespace
        // remains part of the identifier; control characters are rejected.
        var key = (value ?? "").Trim(' ');
        if (key.Length is < 1 or > 25 || key.Any(char.IsControl))
            throw Rule(400, "QC Item 編號必填、最多 25 字元，不可含控制字元。");
        return key;
    }

    private static void Text(NpgsqlCommand command, string value) => command.Parameters.Add("no", NpgsqlDbType.Varchar, 25).Value = value;
    private static Qc201RuleException Rule(int status, string message) => new(status, message);
}
