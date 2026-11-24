using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Inventory;

public sealed class InvProductInRepository(NpgsqlDataSource dataSource)
{
    private const string Combined = """
        SELECT 0::int AS productintype,fgdinno AS productinno,state::int,description,creator,createdate,
               inventoryno,inputdate,source::int,frominventoryno,toerp::int AS toerp,datastamp
        FROM "YMES".tblinvfgdinbasis
        UNION ALL
        SELECT 1::int AS productintype,semiinno AS productinno,state::int,description,creator,createdate,
               inventoryno,inputdate,source::int,frominventoryno,NULL::int AS toerp,datastamp
        FROM "YMES".tblinvsemiinbasis
        """;

    public InvProductInPage List(
        int? productInType,
        int? state,
        int? source,
        string? inventoryNo,
        string? fromInventoryNo,
        string? productInNo,
        int page,
        int pageSize)
    {
        var type = productInType ?? 0;
        var normalizedState = state ?? 0;
        var normalizedSource = source ?? 1;
        if (type is not (0 or 1)) throw Rule(400, "ProductInType 僅接受 0（FGD）或 1（SEMI）。");
        if (normalizedState is < -1 or > 3) throw Rule(400, "State 超出支援範圍。");
        if (normalizedSource is < 0 or > 99) throw Rule(400, "Source 超出支援範圍。");
        if (page is < 1 or > 100_000) throw Rule(400, "Page 必須介於 1 至 100000。");
        if (pageSize is < 1 or > 200) throw Rule(400, "PageSize 必須介於 1 至 200。");
        var offset = checked((page - 1) * pageSize);
        var inventory = Search(inventoryNo, "InventoryNo", 20);
        var fromInventory = Search(fromInventoryNo, "FromInventoryNo", 20);
        var productIn = Search(productInNo, "ProductInNo", 20);

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        using var count = new NpgsqlCommand($"WITH product_in AS ({Combined}) SELECT count(*) FROM product_in WHERE productintype=@type AND state=@state AND source=@source AND (@inventory IS NULL OR position(lower(@inventory) in lower(inventoryno))>0) AND (@from_inventory IS NULL OR position(lower(@from_inventory) in lower(COALESCE(frominventoryno,'')))>0) AND (@number IS NULL OR position(lower(@number) in lower(productinno))>0);", connection, transaction);
        BindFilters(count, type, normalizedState, normalizedSource, inventory, fromInventory, productIn);
        var totalCount = Convert.ToInt64(count.ExecuteScalar());

        using var command = new NpgsqlCommand($"WITH product_in AS ({Combined}) SELECT productintype,productinno,state,description,creator,createdate,inventoryno,inputdate,source,frominventoryno,toerp,datastamp FROM product_in WHERE productintype=@type AND state=@state AND source=@source AND (@inventory IS NULL OR position(lower(@inventory) in lower(inventoryno))>0) AND (@from_inventory IS NULL OR position(lower(@from_inventory) in lower(COALESCE(frominventoryno,'')))>0) AND (@number IS NULL OR position(lower(@number) in lower(productinno))>0) ORDER BY createdate DESC NULLS LAST,productinno COLLATE \"C\" LIMIT @limit OFFSET @offset;", connection, transaction);
        BindFilters(command, type, normalizedState, normalizedSource, inventory, fromInventory, productIn);
        command.Parameters.Add("limit", NpgsqlDbType.Integer).Value = pageSize;
        command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = offset;
        var rows = new List<InvProductInListItem>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) rows.Add(ReadListItem(reader));
        transaction.Commit();
        return new(rows, totalCount, page, pageSize);
    }

    public InvProductInDetails Details(int productInType, string? productInNo)
    {
        if (productInType is not (0 or 1)) throw Rule(400, "ProductInType 僅接受 0（FGD）或 1（SEMI）。");
        var number = Required(productInNo, "ProductInNo", 20);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        var basis = ReadBasis(connection, transaction, productInType, number);
        var details = new List<InvProductInDetailRow>();
        var components = new List<InvProductInComponentRow>();
        if (productInType == 0)
        {
            using var command = new NpgsqlCommand("SELECT productno,productversion,lotno,qty,unitno,locatorno,baselotno,inputdate,frominventoryno FROM \"YMES\".tblinvfgdindetail WHERE fgdinno=@number ORDER BY productno COLLATE \"C\",lotno COLLATE \"C\",locatorno COLLATE \"C\" LIMIT 2001;", connection, transaction);
            AddText(command, "number", number, 20);
            using (var reader = command.ExecuteReader())
                while (reader.Read()) details.Add(ReadDetail(reader));
            if (details.Count > 2000) throw Rule(413, "此單明細超過 2000 筆，請縮小查詢範圍。");
            using var componentCommand = new NpgsqlCommand("SELECT lotno,componentno,goodqty,scrapqty,unitno FROM \"YMES\".tblinvfgdincomponent WHERE fgdinno=@number ORDER BY lotno COLLATE \"C\",componentno COLLATE \"C\" LIMIT 5001;", connection, transaction);
            AddText(componentCommand, "number", number, 20);
            using (var reader = componentCommand.ExecuteReader())
                while (reader.Read()) components.Add(ReadComponent(reader));
        }
        else
        {
            using var command = new NpgsqlCommand("SELECT productno,productversion,lotno,qty,unitno,locatorno,baselotno,NULL::timestamp without time zone,NULL::varchar(20) FROM \"YMES\".tblinvsemiindetail WHERE semiinno=@number ORDER BY productno COLLATE \"C\",lotno COLLATE \"C\",locatorno COLLATE \"C\" LIMIT 2001;", connection, transaction);
            AddText(command, "number", number, 20);
            using (var reader = command.ExecuteReader())
                while (reader.Read()) details.Add(ReadDetail(reader));
            if (details.Count > 2000) throw Rule(413, "此單明細超過 2000 筆，請縮小查詢範圍。");
            using var componentCommand = new NpgsqlCommand("SELECT lotno,componentno,goodqty,scrapqty,unitno FROM \"YMES\".tblinvsemiincomponent WHERE semiinno=@number ORDER BY lotno COLLATE \"C\",componentno COLLATE \"C\" LIMIT 5001;", connection, transaction);
            AddText(componentCommand, "number", number, 20);
            using (var reader = componentCommand.ExecuteReader())
                while (reader.Read()) components.Add(ReadComponent(reader));
        }
        if (components.Count > 5000) throw Rule(413, "此單元件明細超過 5000 筆，請縮小查詢範圍。");
        transaction.Commit();
        return new(basis, details, components);
    }

    private static InvProductInListItem ReadBasis(NpgsqlConnection connection, NpgsqlTransaction transaction, int type, string number)
    {
        var table = type == 0 ? "tblinvfgdinbasis" : "tblinvsemiinbasis";
        var key = type == 0 ? "fgdinno" : "semiinno";
        var toErp = type == 0 ? "toerp::int" : "NULL::int";
        using var command = new NpgsqlCommand($"SELECT {type}::int,{key},state::int,description,creator,createdate,inventoryno,inputdate,source::int,frominventoryno,{toErp},datastamp FROM \"YMES\".{table} WHERE {key}=@number FOR SHARE;", connection, transaction);
        AddText(command, "number", number, 20);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到指定的入庫單。");
        return ReadListItem(reader);
    }

    private static InvProductInListItem ReadListItem(NpgsqlDataReader reader) => new(
        reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2), NullableText(reader, 3), NullableText(reader, 4),
        reader.GetDateTime(5), reader.GetString(6), NullableDate(reader, 7), reader.GetInt32(8), NullableText(reader, 9),
        NullableInt(reader, 10), NullableNumeric(reader, 11));

    private static InvProductInDetailRow ReadDetail(NpgsqlDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetDecimal(3), reader.GetString(4),
        reader.GetString(5), reader.GetString(6), NullableDate(reader, 7), NullableText(reader, 8));

    private static InvProductInComponentRow ReadComponent(NpgsqlDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), NullableNumeric(reader, 2), NullableNumeric(reader, 3), NullableText(reader, 4));

    private static void BindFilters(NpgsqlCommand command, int type, int state, int source,
        string? inventory, string? fromInventory, string? productIn)
    {
        command.Parameters.Add("type", NpgsqlDbType.Integer).Value = type;
        command.Parameters.Add("state", NpgsqlDbType.Integer).Value = state;
        command.Parameters.Add("source", NpgsqlDbType.Integer).Value = source;
        AddText(command, "inventory", inventory, 20);
        AddText(command, "from_inventory", fromInventory, 20);
        AddText(command, "number", productIn, 20);
    }

    private static string? Search(string? value, string field, int length)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > length) throw Rule(400, $"{field} 不可超過 {length} 個字元。");
        return normalized;
    }

    private static string Required(string? value, string field, int length)
    {
        var normalized = Search(value, field, length);
        return normalized ?? throw Rule(400, $"{field} 為必填。");
    }

    private static void AddText(NpgsqlCommand command, string name, string? value, int length) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, length).Value = (object?)value ?? DBNull.Value;

    private static string? NullableText(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static DateTime? NullableDate(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    private static int? NullableInt(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    private static decimal? NullableNumeric(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    private static InvProductInRuleException Rule(int statusCode, string message) => new(statusCode, message);
}
