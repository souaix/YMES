using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Inventory;

public sealed class InvFgdInventoryRepository(NpgsqlDataSource dataSource)
{
    public InvFgdInventoryBasisPage Inventories(int page, int pageSize)
    {
        ValidatePaging(page, pageSize, 100);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        using var count = new NpgsqlCommand("SELECT count(*) FROM \"YMES\".tblinvinventorybasis WHERE inventorytype=1;", connection, transaction);
        var total = Convert.ToInt64(count.ExecuteScalar());
        using var command = new NpgsqlCommand("SELECT inventoryno,inventoryname FROM \"YMES\".tblinvinventorybasis WHERE inventorytype=1 ORDER BY inventoryno COLLATE \"C\" LIMIT @limit OFFSET @offset;", connection, transaction);
        command.Parameters.Add("limit", NpgsqlDbType.Integer).Value = pageSize;
        command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = checked((page - 1) * pageSize);
        var rows = new List<InvFgdInventoryBasis>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) rows.Add(new(reader.GetString(0), Text(reader, 1)));
        transaction.Commit();
        return new(rows, total, page, pageSize);
    }

    public InvFgdInventoryPage Stock(string? inventoryNo, int page, int pageSize)
    {
        var inventory = Required(inventoryNo, "InventoryNo", 20);
        ValidatePaging(page, pageSize, 100);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        EnsureFgdInventory(connection, transaction, inventory);

        using var count = new NpgsqlCommand("SELECT count(*) FROM \"YMES\".tblinvfgdinventory WHERE inventoryno=@inventory AND locqty>0;", connection, transaction);
        AddText(count, "inventory", inventory, 20);
        var total = Convert.ToInt64(count.ExecuteScalar());

        using var command = new NpgsqlCommand("""
            SELECT inventoryno,productno,locatorno,lotno,locqty,unitno,COALESCE(belongtotype,-1)::int,belongtono,inputdate,fgdinno
            FROM "YMES".tblinvfgdinventory
            WHERE inventoryno=@inventory AND locqty>0
            ORDER BY productno COLLATE "C",lotno COLLATE "C",locatorno COLLATE "C"
            LIMIT @limit OFFSET @offset;
            """, connection, transaction);
        AddText(command, "inventory", inventory, 20);
        command.Parameters.Add("limit", NpgsqlDbType.Integer).Value = pageSize;
        command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = checked((page - 1) * pageSize);
        var rows = new List<InvFgdInventoryRow>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) rows.Add(ReadStock(reader));
        transaction.Commit();
        return new(rows, total, page, pageSize);
    }

    public InvFgdInventoryDetails StockDetails(string? inventoryNo, string? lotNo, string? locatorNo)
    {
        var inventory = Required(inventoryNo, "InventoryNo", 20);
        var lot = Required(lotNo, "LotNo", 50);
        var locator = Required(locatorNo, "LocatorNo", 20);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        EnsureFgdInventory(connection, transaction, inventory);
        using (var stock = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblinvfgdinventory WHERE inventoryno=@inventory AND lotno=@lot AND locatorno=@locator AND locqty>0;", connection, transaction))
        {
            AddText(stock, "inventory", inventory, 20);
            AddText(stock, "lot", lot, 50);
            AddText(stock, "locator", locator, 20);
            if (stock.ExecuteScalar() is null) throw Rule(404, "找不到指定的正數成品庫存列。");
        }

        using var componentCommand = new NpgsqlCommand("""
            SELECT componentno,goodqty,scrapqty,unitno
            FROM "YMES".tblinvfgdinventorycomponent
            WHERE inventoryno=@inventory AND lotno=@lot AND locatorno=@locator
            ORDER BY componentno COLLATE "C" LIMIT 1001;
            """, connection, transaction);
        AddText(componentCommand, "inventory", inventory, 20);
        AddText(componentCommand, "lot", lot, 50);
        AddText(componentCommand, "locator", locator, 20);
        var components = new List<InvFgdInventoryComponent>();
        using (var reader = componentCommand.ExecuteReader())
            while (reader.Read()) components.Add(new(reader.GetString(0), Numeric(reader, 1), Numeric(reader, 2), Text(reader, 3)));
        if (components.Count > 1000) throw Rule(413, "此庫存列的 Component 明細超過 1000 筆。");

        IReadOnlyList<InvFgdBinDefinition> binDefinitions = [];
        InvFgdGoodBinState? goodBinState = null;
        if (components.Count == 0)
        {
            using var binCommand = new NpgsqlCommand("SELECT binno,COALESCE(binorder,99)::int FROM \"YMES\".tblsysbin ORDER BY COALESCE(binorder,99),binno COLLATE \"C\" LIMIT 101;", connection, transaction);
            var bins = new List<InvFgdBinDefinition>();
            using (var binReader = binCommand.ExecuteReader())
                while (binReader.Read()) bins.Add(new(binReader.GetString(0), binReader.GetInt32(1)));
            if (bins.Count > 100) throw Rule(413, "系統 Bin 定義超過 100 筆。");
            binDefinitions = bins;

            using var goodBinCommand = new NpgsqlCommand("SELECT lotserial,bin1,bin2,bin3,bin4,bin5,bin6,bin7,bin8 FROM \"YMES\".tblwipgoodbinstate WHERE lotno=@lot;", connection, transaction);
            AddText(goodBinCommand, "lot", lot, 50);
            using var goodBinReader = goodBinCommand.ExecuteReader();
            if (goodBinReader.Read())
            {
                var quantities = Enumerable.Range(1, 8)
                    .Select(index => new InvFgdBinQuantity($"bin{index}", Numeric(goodBinReader, index)))
                    .ToArray();
                goodBinState = new(lot, Text(goodBinReader, 0), quantities);
            }
        }
        transaction.Commit();
        return new(components, binDefinitions, goodBinState);
    }

    public InvFgdInPage Confirmations(int page, int pageSize)
    {
        ValidatePaging(page, pageSize, 100);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        using var count = new NpgsqlCommand("SELECT count(*) FROM \"YMES\".tblinvfgdinbasis WHERE state=0 AND COALESCE(toerp,0)<>1;", connection, transaction);
        var total = Convert.ToInt64(count.ExecuteScalar());
        using var command = new NpgsqlCommand("""
            SELECT fgdinno,state::int,description,creator,createdate,inventoryno,inputdate,source::int,frominventoryno,toerp::int,datastamp
            FROM "YMES".tblinvfgdinbasis
            WHERE state=0 AND COALESCE(toerp,0)<>1
            ORDER BY fgdinno COLLATE "C"
            LIMIT @limit OFFSET @offset;
            """, connection, transaction);
        command.Parameters.Add("limit", NpgsqlDbType.Integer).Value = pageSize;
        command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = checked((page - 1) * pageSize);
        var rows = new List<InvFgdInConfirmation>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) rows.Add(ReadConfirmation(reader));
        transaction.Commit();
        return new(rows, total, page, pageSize);
    }

    public InvFgdInConfirmationDetails ConfirmationDetails(string? fgdInNo)
    {
        var number = Required(fgdInNo, "FgdInNo", 20);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        InvFgdInConfirmation basis;
        using (var command = new NpgsqlCommand("""
            SELECT fgdinno,state::int,description,creator,createdate,inventoryno,inputdate,source::int,frominventoryno,toerp::int,datastamp
            FROM "YMES".tblinvfgdinbasis WHERE fgdinno=@number;
            """, connection, transaction))
        {
            AddText(command, "number", number, 20);
            using var reader = command.ExecuteReader();
            if (!reader.Read()) throw Rule(404, "找不到指定的成品入庫單。");
            basis = ReadConfirmation(reader);
        }

        using var detailCommand = new NpgsqlCommand("""
            SELECT productno,productversion,lotno,qty,unitno,locatorno,baselotno,inputdate,frominventoryno
            FROM "YMES".tblinvfgdindetail WHERE fgdinno=@number
            ORDER BY lotno COLLATE "C",locatorno COLLATE "C" LIMIT 501;
            """, connection, transaction);
        AddText(detailCommand, "number", number, 20);
        var details = new List<InvFgdInDetail>();
        using (var reader = detailCommand.ExecuteReader())
            while (reader.Read()) details.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetDecimal(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), Date(reader, 7), Text(reader, 8)));
        if (details.Count > 500) throw Rule(413, "此成品入庫單明細超過 500 筆。");

        using var componentCommand = new NpgsqlCommand("""
            SELECT lotno,componentno,goodqty,scrapqty,unitno
            FROM "YMES".tblinvfgdincomponent WHERE fgdinno=@number
            ORDER BY lotno COLLATE "C",componentno COLLATE "C" LIMIT 5001;
            """, connection, transaction);
        AddText(componentCommand, "number", number, 20);
        var components = new List<InvFgdInComponent>();
        using (var reader = componentCommand.ExecuteReader())
            while (reader.Read()) components.Add(new(reader.GetString(0), reader.GetString(1), Numeric(reader, 2), Numeric(reader, 3), Text(reader, 4)));
        if (components.Count > 5000) throw Rule(413, "此成品入庫單 Component 明細超過 5000 筆。");
        transaction.Commit();
        return new(basis, details, components);
    }

    private static void EnsureFgdInventory(NpgsqlConnection connection, NpgsqlTransaction transaction, string inventoryNo)
    {
        using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblinvinventorybasis WHERE inventoryno=@inventory AND inventorytype=1;", connection, transaction);
        AddText(command, "inventory", inventoryNo, 20);
        if (command.ExecuteScalar() is null) throw Rule(404, "找不到成品庫房（InventoryType=1）。");
    }

    private static InvFgdInventoryRow ReadStock(NpgsqlDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetDecimal(4), Text(reader, 5),
        reader.GetInt32(6), Text(reader, 7), Date(reader, 8), Text(reader, 9));

    private static InvFgdInConfirmation ReadConfirmation(NpgsqlDataReader reader) => new(
        reader.GetString(0), reader.GetInt32(1), Text(reader, 2), Text(reader, 3), reader.GetDateTime(4), reader.GetString(5),
        Date(reader, 6), reader.GetInt32(7), Text(reader, 8), Int(reader, 9), Numeric(reader, 10));

    private static void ValidatePaging(int page, int pageSize, int maxPageSize)
    {
        if (page is < 1 or > 100_000) throw Rule(400, "Page 必須介於 1 至 100000。");
        if (pageSize is < 1 || pageSize > maxPageSize) throw Rule(400, $"PageSize 必須介於 1 至 {maxPageSize}。");
    }

    private static string Required(string? value, string field, int length)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) throw Rule(400, $"{field} 為必填。");
        if (normalized.Length > length) throw Rule(400, $"{field} 不可超過 {length} 個字元。");
        return normalized;
    }

    private static void AddText(NpgsqlCommand command, string name, string value, int length) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, length).Value = value;

    private static string? Text(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static DateTime? Date(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    private static decimal? Numeric(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    private static int? Int(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    private static InvFgdInventoryRuleException Rule(int statusCode, string message) => new(statusCode, message);
}
