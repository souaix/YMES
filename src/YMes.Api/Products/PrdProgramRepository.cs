using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MES.Api.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Products;

public sealed class PrdProgramRepository(NpgsqlDataSource dataSource)
{
    public IReadOnlyList<PrdProgramOption> Options()
    {
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblprdprogrambasis");
        // WinForms LoadProgram() supplies all states, not only approved rows.
        using var command = new NpgsqlCommand("SELECT programno,programtype,description,issuestate::int FROM \"YMES\".tblprdprogrambasis ORDER BY programno;", connection);
        var result = new List<PrdProgramOption>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(new(reader.GetString(0), TextOrNull(reader, 1), TextOrNull(reader, 2), reader.GetInt32(3)));
        return result;
    }

    public PrdProgramAssignments Get(string productNo, string version)
    {
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var product = PostgresMasterDataRepository.ReadProductVersionContext(connection, transaction, productNo, version, false);
        EnsureTable(connection, transaction, "tblprdproductprograms");
        var result = Snapshot(product, ReadAssignments(connection, transaction, product));
        transaction.Commit();
        return result;
    }

    public PrdProgramAssignments Replace(string productNo, string version, PrdProgramWriteRequest request, string actor)
    {
        actor = (actor ?? "").Trim();
        if (actor.Length is < 1 or > 10 || actor.Any(char.IsControl)) throw Rule(401, "登入帳號無效或超過 10 字元。");
        if (request.ProgramNos is null) throw Rule(400, "必須明確提供 programNos 陣列；清除全部請傳 []。");
        if (request.ProgramNos.Count > 10000) throw Rule(400, "單次最多指派 10000 個 Program。");
        if (request.Version is null || request.Version.Length != 64 || !request.Version.All(Uri.IsHexDigit))
            throw Rule(400, "必須提供載入指派清單時取得的完整 version。");
        var requested = request.ProgramNos.Select(Key).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            // Serialize all version-owned child writes with the real product
            // master. State is always read from this lock, never the Vue prop.
            var product = PostgresMasterDataRepository.ReadProductVersionContext(connection, transaction, productNo, version, true);
            EnsureTable(connection, transaction, "tblprdproductprograms");
            var current = Snapshot(product, ReadAssignments(connection, transaction, product));
            if (!string.Equals(current.Version, request.Version, StringComparison.Ordinal))
                throw Rule(409, "產品狀態、版本資料或 Program 指派已變更，請關閉後重新載入再儲存。");
            if (product.IssueState != 0) throw Rule(409, "只有未核准（狀態 0）的產品版次可以儲存 Program 指派。");
            var selected = new List<string>();
            if (requested.Length > 0)
            {
                EnsureTable(connection, transaction, "tblprdprogrambasis");
                foreach (var no in requested)
                {
                    using var check = new NpgsqlCommand("SELECT programno FROM \"YMES\".tblprdprogrambasis WHERE lower(programno)=lower(@program) FOR SHARE;", connection, transaction);
                    Text(check, "program", no, 50);
                    using var reader = check.ExecuteReader();
                    if (!reader.Read()) throw Rule(422, $"Program {no} 不存在，請重新載入並選取。");
                    var stored = reader.GetString(0);
                    if (reader.Read()) throw Rule(409, $"Program {no} 存在大小寫不同的重複主檔，請先整理。");
                    selected.Add(stored);
                }
            }
            DeleteAssignments(connection, transaction, product.ProductNo, product.ProductVersion);
            foreach (var no in selected)
            {
                using var insert = new NpgsqlCommand("INSERT INTO \"YMES\".tblprdproductprograms(productno,productversion,programno) VALUES(@product,@version,@program);", connection, transaction);
                BindProduct(insert, product.ProductNo, product.ProductVersion); Text(insert, "program", no, 50);
                insert.ExecuteNonQuery();
            }
            var result = Snapshot(product, ReadAssignments(connection, transaction, product));
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    // Called by the existing product CreateProductVersion transaction only.
    // Caller locks the canonical source first, inserts/locks target, then copies
    // RecipeGroup -> Program in a fixed order. This does not implement generic
    // CopyProduct/detailSource behavior, which still needs its own source audit.
    public static void CopyAssignmentsForVersion(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string productNo, string sourceVersion, string targetVersion)
    {
        try
        {
            // An absent optional module table has no persisted assignments to
            // copy. A present table with missing permissions must fail, not skip.
            if (!TableExists(connection, transaction, "tblprdproductprograms")) return;
            if (string.Equals(sourceVersion, targetVersion, StringComparison.OrdinalIgnoreCase))
                throw Rule(400, "Program 複製的來源與目的產品版次不可相同。");
            var programs = new List<string>();
            using (var source = new NpgsqlCommand("SELECT programno FROM \"YMES\".tblprdproductprograms WHERE lower(productno)=lower(@product) AND lower(productversion)=lower(@version) ORDER BY programno;", connection, transaction))
            {
                BindProduct(source, productNo, sourceVersion);
                using var reader = source.ExecuteReader();
                while (reader.Read()) programs.Add(reader.GetString(0));
            }
            if (programs.Count > 0)
            {
                EnsureTable(connection, transaction, "tblprdprogrambasis");
                using var guard = new NpgsqlCommand("SELECT programno FROM \"YMES\".tblprdprogrambasis WHERE lower(programno)=ANY(@programs) ORDER BY programno FOR SHARE;", connection, transaction);
                guard.Parameters.Add("programs", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = programs.Select(x => x.ToLowerInvariant()).Distinct().ToArray();
                var existing = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                using (var reader = guard.ExecuteReader())
                    while (reader.Read()) { var code = reader.GetString(0); existing[code] = existing.GetValueOrDefault(code) + 1; }
                if (programs.Any(code => !existing.ContainsKey(code))) throw Rule(422, "來源產品有已不存在的 Program 主檔，請先整理指派；新產品版本交易未完成。");
                if (existing.Values.Any(count => count > 1)) throw Rule(409, "來源 Program 主檔有大小寫不同的重複鍵，請先整理；新產品版本交易未完成。");
            }
            DeleteAssignments(connection, transaction, productNo, targetVersion);
            foreach (var code in programs)
            {
                using var command = new NpgsqlCommand("INSERT INTO \"YMES\".tblprdproductprograms(productno,productversion,programno) VALUES(@product,@version,@program);", connection, transaction);
                BindProduct(command, productNo, targetVersion); Text(command, "program", code, 50);
                command.ExecuteNonQuery();
            }
        }
        catch (NpgsqlException)
        {
            throw Rule(503, "建立新產品版次時複製 Program 指派失敗；本次新版本交易未完成，請確認資料表與權限。");
        }
    }

    // Existing product owner calls this while holding its canonical parent row
    // FOR UPDATE. No source-absent cascade is inferred for assignment rows.
    public static void EnsureNoAssignmentsForProductDelete(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string productNo, string version)
    {
        try
        {
            if (!TableExists(connection, transaction, "tblprdproductprograms")) return;
            using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblprdproductprograms WHERE lower(productno)=lower(@product) AND lower(productversion)=lower(@version) LIMIT 1;", connection, transaction);
            BindProduct(command, productNo, version);
            if (command.ExecuteScalar() is not null) throw Rule(409, "此產品版次仍有 Program 指派，不能實體刪除；請先依產品維護流程清除指派。");
        }
        catch (NpgsqlException)
        {
            throw Rule(503, "無法確認產品版次是否仍有 Program 指派；產品未刪除，請確認資料表與讀取權限。");
        }
    }

    private static IReadOnlyList<string> ReadAssignments(NpgsqlConnection connection, NpgsqlTransaction transaction, ProductVersionContext product)
    {
        using var command = new NpgsqlCommand("SELECT programno FROM \"YMES\".tblprdproductprograms WHERE lower(productno)=lower(@product) AND lower(productversion)=lower(@version) ORDER BY programno;", connection, transaction);
        BindProduct(command, product.ProductNo, product.ProductVersion);
        var result = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(reader.GetString(0));
        return result;
    }

    private static PrdProgramAssignments Snapshot(ProductVersionContext product, IReadOnlyList<string> programs)
    {
        var json = JsonSerializer.Serialize(new { Product = product, Programs = programs });
        var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        return new(product.ProductNo, product.ProductVersion, product.IssueState, programs, version);
    }

    private static void DeleteAssignments(NpgsqlConnection connection, NpgsqlTransaction transaction, string productNo, string version)
    {
        using var command = new NpgsqlCommand("DELETE FROM \"YMES\".tblprdproductprograms WHERE lower(productno)=lower(@product) AND lower(productversion)=lower(@version);", connection, transaction);
        BindProduct(command, productNo, version); command.ExecuteNonQuery();
    }

    private static string Key(string? value)
    {
        var code = (value ?? "").Trim();
        if (code.Length is < 1 or > 50 || code.Any(char.IsControl)) throw Rule(400, "ProgramNo 為必填、最長 50 字元，不可含控制字元。");
        return code;
    }

    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string name)
    {
        // pg_catalog existence is independent of table-level grants; unlike an
        // information_schema visibility check, missing SELECT cannot look absent.
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@name);", connection, transaction);
        Text(command, "name", name, 63); return command.ExecuteScalar() is true;
    }
    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction, string name)
    {
        if (!TableExists(connection, transaction, name)) throw Rule(503, $"YMES 尚未建立 {name}，請先比對來源 DDL 與部署 migration。");
    }
    private static void BindProduct(NpgsqlCommand command, string productNo, string version)
    {
        Text(command, "product", productNo, 50); Text(command, "version", version, 5);
    }
    private static void Text(NpgsqlCommand command, string name, string? value, int size) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = (object?)value ?? DBNull.Value;
    private static string? TextOrNull(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static void Rollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { /* Preserve original failure. */ } }
    private static PrdProgramRuleException Rule(int status, string message) => new(status, message);
}
