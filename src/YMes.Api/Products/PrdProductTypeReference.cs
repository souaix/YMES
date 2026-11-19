using MES.Api.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Products;

/// <summary>Shared creation namespace and approved Type reference locks for product writers.</summary>
public static class PrdProductTypeReference
{
    // New Product and Type creators acquire this before any identity/row lock.
    // Existing product updates/Version do not introduce a new ProductNo and
    // retain their original product-row -> Type SHARE ordering.
    public static void LockCreationNamespace(NpgsqlConnection connection, NpgsqlTransaction transaction, string productNoOrType)
    {
        var value = Key(productNoOrType);
        using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:PRD:ProductTypeProductNamespace:' || lower(btrim(@no)),0));", connection, transaction);
        Text(command, "no", value, 50); command.ExecuteNonQuery();
    }

    public static void EnsureNewProductNamespace(NpgsqlConnection connection, NpgsqlTransaction transaction, string productNo)
    {
        var value = Key(productNo);
        using (var type = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblprdtype WHERE lower(btrim(producttype))=lower(@no));", connection, transaction))
        {
            Text(type, "no", value, 50);
            if ((bool)(type.ExecuteScalar() ?? false)) throw new MasterDataRuleException(409, "產品編號不可與產品類別編號重複。");
        }
        // The original module catalog is optional until PD103 is deployed.
        // Inspect pg_catalog, not privilege-filtered information_schema. An
        // existing but unreadable catalog must fail closed through Npgsql/503.
        using var exists = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='YMES' AND c.relname='tblprdmodulebasis' AND c.relkind IN ('r','p'));
            """, connection, transaction);
        if ((bool)(exists.ExecuteScalar() ?? false))
        {
            using var module = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblprdmodulebasis WHERE lower(btrim(moduleno))=lower(@no));", connection, transaction);
            Text(module, "no", value, 50);
            if ((bool)(module.ExecuteScalar() ?? false)) throw new MasterDataRuleException(409, "產品編號不可與製程模組編號重複。");
        }
    }

    internal static void EnsureNewTypeNamespace(NpgsqlConnection connection, NpgsqlTransaction transaction, string productType)
    {
        var value = Key(productType);
        using var product = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblprdproductbasis WHERE lower(btrim(productno))=lower(@no));", connection, transaction);
        Text(product, "no", value, 50);
        if ((bool)(product.ExecuteScalar() ?? false)) throw new MasterDataRuleException(409, "產品類別編號不可與既有產品編號重複，請使用其他編號。");
    }

    public static string LockApproved(NpgsqlConnection connection, NpgsqlTransaction transaction, string? requested)
    {
        var value = Key(requested);
        using var command = new NpgsqlCommand("""
            SELECT producttype,issuestate FROM "YMES".tblprdtype
            WHERE lower(btrim(producttype))=lower(@type) ORDER BY producttype FOR SHARE;
            """, connection, transaction);
        Text(command, "type", value, 50);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new MasterDataRuleException(422, "找不到指定產品類別，請重新選擇已核准的產品類別。");
        var canonical = reader.GetString(0);
        var active = !reader.IsDBNull(1) && reader.GetDecimal(1) == 2;
        if (reader.Read()) throw new MasterDataRuleException(409, "產品類別存在大小寫或空白重複，請先整理主檔。");
        if (!active) throw new MasterDataRuleException(422, "新增或更換產品類別必須選擇已核准的類別。");
        return canonical;
    }

    public static string ResolveForUpdate(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string? requested, string existing)
    {
        // An unchanged legacy value need not become a valid new catalog choice.
        // Return the server's exact value, including case/whitespace/empty text.
        if (string.Equals(requested?.Trim() ?? "", existing.Trim(), StringComparison.OrdinalIgnoreCase)) return existing;
        return LockApproved(connection, transaction, requested);
    }

    internal static string Key(string? value)
    {
        var key = value?.Trim() ?? "";
        if (key.Length is < 1 or > 50 || key.Any(char.IsControl))
            throw new MasterDataRuleException(400, "產品類別必填，最多 50 個字元，且不可含控制字元。");
        return key;
    }

    internal static void Text(NpgsqlCommand command, string name, string? value, int size) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = (object?)value ?? DBNull.Value;
}
