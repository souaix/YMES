using MES.Api.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Products;

public sealed class PrdRecipeGroupRepository(NpgsqlDataSource dataSource)
{
    public IReadOnlyList<PrdRecipeGroupAssignmentDto> List(string no, string version)
    {
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var product = PostgresMasterDataRepository.ReadProductVersionContext(connection, transaction, no, version, forUpdate: false);
        EnsureTable(connection, transaction, "tblprdrecipe");
        using var command = new NpgsqlCommand("""
            SELECT productno,productversion,recipegroup FROM "YMES".tblprdrecipe
            WHERE lower(productno)=lower(@no) AND lower(productversion)=lower(@version)
            ORDER BY recipegroup;
            """, connection, transaction);
        ProductParameters(command, product.ProductNo, product.ProductVersion);
        var result = new List<PrdRecipeGroupAssignmentDto>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) result.Add(Read(reader));
        transaction.Commit();
        return result;
    }

    public IReadOnlyList<PrdRecipeGroupOptionDto> Options(string no, string version)
    {
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        _ = PostgresMasterDataRepository.ReadProductVersionContext(connection, transaction, no, version, forUpdate: false);
        EnsureTable(connection, transaction, "tblprdrecipebasis");
        using var command = new NpgsqlCommand("""
            SELECT recipegroup,recipeversion::int,description,issuestate::int,curversion::int
            FROM "YMES".tblprdrecipebasis WHERE issuestate=2 AND curversion=1
            ORDER BY recipegroup,recipeversion;
            """, connection, transaction);
        var result = new List<PrdRecipeGroupOptionDto>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) result.Add(new(reader.GetString(0), reader.GetInt32(1),
                reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetInt32(3), reader.GetInt32(4)));
        transaction.Commit();
        return result;
    }

    public PrdRecipeGroupAssignmentDto Create(string no, string version, PrdRecipeGroupWriteRequest request, string actor)
    {
        ValidateActor(actor);
        var group = Required(request.RecipeGroup, "RecipeGroup", 50);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            // Legacy RecipeGroup assignment has no product IssueState gate.
            // The canonical product mutex coordinates assignment and version writes.
            var product = PostgresMasterDataRepository.ReadProductVersionContext(connection, transaction, no, version, forUpdate: true);
            EnsureTable(connection, transaction, "tblprdrecipe");
            EnsureTable(connection, transaction, "tblprdrecipebasis");
            group = ReadApprovedCurrentGroup(connection, transaction, group);
            using (var duplicate = new NpgsqlCommand("""
                SELECT 1 FROM "YMES".tblprdrecipe
                WHERE lower(productno)=lower(@no) AND lower(productversion)=lower(@version)
                  AND lower(recipegroup)=lower(@group);
                """, connection, transaction))
            {
                ProductParameters(duplicate, product.ProductNo, product.ProductVersion); Text(duplicate, "group", group, 50);
                if (duplicate.ExecuteScalar() is not null) throw Rule(409, "此產品版本已指派這個 RecipeGroup。");
            }
            using var insert = new NpgsqlCommand("""
                INSERT INTO "YMES".tblprdrecipe(productno,productversion,recipegroup)
                VALUES(@no,@version,@group) RETURNING productno,productversion,recipegroup;
                """, connection, transaction);
            ProductParameters(insert, product.ProductNo, product.ProductVersion); Text(insert, "group", group, 50);
            PrdRecipeGroupAssignmentDto result;
            using (var reader = insert.ExecuteReader()) { reader.Read(); result = Read(reader); }
            transaction.Commit();
            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Rollback(transaction); throw Rule(409, "此產品版本已指派這個 RecipeGroup。");
        }
        catch { Rollback(transaction); throw; }
    }

    public PrdRecipeGroupAssignmentDto Delete(string no, string version, string recipeGroup, string actor)
    {
        ValidateActor(actor);
        var group = Required(recipeGroup, "RecipeGroup", 50);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var product = PostgresMasterDataRepository.ReadProductVersionContext(connection, transaction, no, version, forUpdate: true);
            EnsureTable(connection, transaction, "tblprdrecipe");
            PrdRecipeGroupAssignmentDto target;
            using (var lookup = new NpgsqlCommand("""
                SELECT productno,productversion,recipegroup FROM "YMES".tblprdrecipe
                WHERE lower(productno)=lower(@no) AND lower(productversion)=lower(@version)
                  AND lower(recipegroup)=lower(@group)
                ORDER BY productno,productversion,recipegroup FOR UPDATE;
                """, connection, transaction))
            {
                ProductParameters(lookup, product.ProductNo, product.ProductVersion); Text(lookup, "group", group, 50);
                using var reader = lookup.ExecuteReader();
                if (!reader.Read()) throw Rule(404, "找不到指定產品版本的 RecipeGroup 指派。");
                target = Read(reader);
                if (reader.Read()) throw Rule(409, "RecipeGroup 指派包含僅大小寫不同的重複鍵，請先整理資料。");
            }
            using var delete = new NpgsqlCommand("""
                DELETE FROM "YMES".tblprdrecipe
                WHERE productno=@no AND productversion=@version AND recipegroup=@group
                RETURNING productno,productversion,recipegroup;
                """, connection, transaction);
            ProductParameters(delete, target.ProductNo, target.ProductVersion); Text(delete, "group", target.RecipeGroup, 50);
            using (var reader = delete.ExecuteReader())
                if (!reader.Read()) throw Rule(409, "RecipeGroup 指派已變更，請重新讀取清單。");
            transaction.Commit();
            return target;
        }
        catch { Rollback(transaction); throw; }
    }

    // The caller owns source/target product locks. Copy exact assignments,
    // including formerly approved groups, as the source CopyPRDRecipe does.
    public static void CopyAssignmentsForVersion(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string productNo, string sourceVersion, string targetVersion)
    {
        try { CopyAssignments(connection, transaction, productNo, sourceVersion, targetVersion); }
        catch (NpgsqlException) { throw Rule(503, "建立新產品版次時複製 RecipeGroup 指派失敗；本次版本交易未完成，請確認資料表與權限。"); }
    }

    private static void CopyAssignments(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string productNo, string sourceVersion, string targetVersion)
    {
        if (string.Equals(sourceVersion, targetVersion, StringComparison.OrdinalIgnoreCase)) throw Rule(400, "RecipeGroup 複製的來源與目的版次不可相同。");
        // An absent optional module has no existing links to copy. Once its
        // assignment table exists, schema/permission failures are never ignored.
        if (!TableExists(connection, transaction, "tblprdrecipe")) return;
        var groups = new List<string>();
        using (var source = new NpgsqlCommand("""
            SELECT recipegroup FROM "YMES".tblprdrecipe
            WHERE lower(productno)=lower(@no) AND lower(productversion)=lower(@version)
            ORDER BY recipegroup;
            """, connection, transaction))
        {
            ProductParameters(source, productNo, sourceVersion);
            using var reader = source.ExecuteReader();
            while (reader.Read()) groups.Add(reader.GetString(0));
        }
        if (groups.Count > 0)
        {
            EnsureTable(connection, transaction, "tblprdrecipebasis");
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var guard = new NpgsqlCommand("""
                SELECT recipegroup FROM "YMES".tblprdrecipebasis WHERE lower(recipegroup)=ANY(@groups)
                ORDER BY recipegroup,recipeversion FOR SHARE;
                """, connection, transaction);
            guard.Parameters.Add("groups", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = groups.Select(x => x.ToLowerInvariant()).Distinct().ToArray();
            using (var reader = guard.ExecuteReader()) while (reader.Read()) existing.Add(reader.GetString(0));
            if (groups.Any(group => !existing.Contains(group))) throw Rule(422, "來源產品 RecipeGroup 的主檔已不存在，版本交易未寫入。");
        }
        using (var clear = new NpgsqlCommand("DELETE FROM \"YMES\".tblprdrecipe WHERE productno=@no AND productversion=@version;", connection, transaction))
        {
            ProductParameters(clear, productNo, targetVersion); clear.ExecuteNonQuery();
        }
        foreach (var group in groups)
        {
            using var copy = new NpgsqlCommand("INSERT INTO \"YMES\".tblprdrecipe(productno,productversion,recipegroup) VALUES(@no,@version,@group);", connection, transaction);
            ProductParameters(copy, productNo, targetVersion); Text(copy, "group", group, 50); copy.ExecuteNonQuery();
        }
    }

    // Same parent lock as the assignment writers; never leave orphan links.
    public static void EnsureNoAssignmentsForProductDelete(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string productNo, string version)
    {
        try
        {
            if (!TableExists(connection, transaction, "tblprdrecipe")) return;
            using var command = new NpgsqlCommand("""
                SELECT 1 FROM "YMES".tblprdrecipe
                WHERE lower(productno)=lower(@no) AND lower(productversion)=lower(@version) LIMIT 1;
                """, connection, transaction);
            ProductParameters(command, productNo, version);
            if (command.ExecuteScalar() is not null) throw Rule(409, "產品版本仍有 RecipeGroup 指派，請先移除指派後再刪除產品。");
        }
        catch (NpgsqlException) { throw Rule(503, "無法檢查產品 RecipeGroup 指派；產品未刪除，請確認資料表與權限。"); }
    }

    private static string ReadApprovedCurrentGroup(NpgsqlConnection connection, NpgsqlTransaction transaction, string group)
    {
        using var command = new NpgsqlCommand("""
            SELECT recipegroup FROM "YMES".tblprdrecipebasis
            WHERE lower(recipegroup)=lower(@group) AND issuestate=2 AND curversion=1
            ORDER BY recipegroup,recipeversion FOR SHARE;
            """, connection, transaction);
        Text(command, "group", group, 50);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(422, "RecipeGroup 必須是已核准的目前版本。");
        var canonical = reader.GetString(0);
        if (reader.Read()) throw Rule(409, "RecipeGroup 有多個已核准目前版本，請先整理主檔。");
        return canonical;
    }

    private static PrdRecipeGroupAssignmentDto Read(NpgsqlDataReader reader) => new(reader.GetString(0), reader.GetString(1), reader.GetString(2));
    private static string Required(string? input, string field, int maximum)
    {
        var value = input?.Trim() ?? "";
        if (value.Length is 0 || value.Length > maximum || value.Any(char.IsControl)) throw Rule(400, $"{field} 必填，且不可超過 {maximum} 字元或含控制字元。");
        return value;
    }
    private static void ValidateActor(string? actor)
    {
        var value = actor?.Trim() ?? "";
        if (value.Length is < 1 or > 10 || value.Any(char.IsControl)) throw Rule(401, "登入帳號無效或超過 10 字元。");
    }
    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction transaction, string table)
    {
        if (!TableExists(connection, transaction, table)) throw Rule(503, $"YMES 尚未建立 {table}，請先核對產品 RecipeGroup schema。");
    }
    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@table AND c.relkind IN ('r','p'));", connection, transaction);
        Text(command, "table", table, 63); return command.ExecuteScalar() is true;
    }
    private static void ProductParameters(NpgsqlCommand command, string no, string version) { Text(command, "no", no, 50); Text(command, "version", version, 5); }
    private static void Text(NpgsqlCommand command, string parameter, string value, int maximum) => command.Parameters.Add(parameter, NpgsqlDbType.Varchar, maximum).Value = value;
    private static PrdRecipeGroupRuleException Rule(int status, string message) => new(status, message);
    private static void Rollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { } }
}
