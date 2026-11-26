using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Inventory;

public sealed class InvProductInLogRepository(NpgsqlDataSource dataSource)
{
    private const string Combined = """
        SELECT 0::int AS productintype,fgdinno AS productinno,state::int,description,creator,createdate,
               inventoryno,inputdate,source::int,frominventoryno,reviser,revisedate,toerp::int,
               erpno,canceluser,canceldate,datastamp
        FROM "YMES".tblinvfgdinbasis
        UNION ALL
        SELECT 1::int AS productintype,semiinno AS productinno,state::int,description,creator,createdate,
               inventoryno,inputdate,source::int,frominventoryno,reviser,revisedate,NULL::int,
               NULL::varchar(20) AS erpno,NULL::varchar(10) AS canceluser,
               NULL::timestamp without time zone AS canceldate,datastamp
        FROM "YMES".tblinvsemiinbasis
        """;

    public InvProductInLogPage List(
        int? productInType,
        int? state,
        string? productInNo,
        string? inventoryNo,
        string? fromInventoryNo,
        DateOnly? createDateFrom,
        DateOnly? createDateTo,
        DateOnly? inputDateFrom,
        DateOnly? inputDateTo,
        int page,
        int pageSize)
    {
        if (productInType.HasValue && productInType.Value is not (0 or 1))
            throw Rule(400, "ProductInType 僅接受 0（FGD）或 1（SEMI）。");
        if (state.HasValue && state.Value is not (-1 or 0 or 1 or 2 or 3))
            throw Rule(400, "State 僅接受 -1、0、1、2 或 3。");
        ValidatePaging(page, pageSize);

        var number = Search(productInNo, "ProductInNo", 20);
        var inventory = Search(inventoryNo, "InventoryNo", 20);
        var fromInventory = Search(fromInventoryNo, "FromInventoryNo", 20);
        var createRange = BuildDateRange(createDateFrom, createDateTo, "CreateDate");
        var inputRange = BuildDateRange(inputDateFrom, inputDateTo, "InputDate");
        if (number is null && inventory is null && fromInventory is null && state is null &&
            createRange is null && inputRange is null)
            throw Rule(400, "請至少設定單號、庫房、狀態或一組日期範圍後再查詢。");

        var offset = checked((page - 1) * pageSize);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        const string where = "(@type IS NULL OR productintype=@type) AND (@state IS NULL OR state=@state) " +
            "AND (@number IS NULL OR productinno=@number) AND (@inventory IS NULL OR inventoryno=@inventory) " +
            "AND (@from_inventory IS NULL OR frominventoryno=@from_inventory) " +
            "AND (@create_from IS NULL OR createdate>=@create_from) " +
            "AND (@create_to IS NULL OR createdate<@create_to) " +
            "AND (@input_from IS NULL OR inputdate>=@input_from) " +
            "AND (@input_to IS NULL OR inputdate<@input_to)";

        using var count = new NpgsqlCommand($"WITH product_in AS ({Combined}) SELECT count(*) FROM product_in WHERE {where};", connection, transaction);
        AddFilters(count, productInType, state, number, inventory, fromInventory, createRange, inputRange);
        var totalCount = Convert.ToInt64(count.ExecuteScalar());

        using var command = new NpgsqlCommand($"""
            WITH product_in AS ({Combined})
            SELECT productintype,productinno,state,description,creator,createdate,inventoryno,inputdate,source,
                   frominventoryno,reviser,revisedate,toerp,erpno,canceluser,canceldate,datastamp
            FROM product_in
            WHERE {where}
            ORDER BY createdate DESC NULLS LAST,productinno COLLATE "C",productintype
            LIMIT @limit OFFSET @offset;
            """, connection, transaction);
        AddFilters(command, productInType, state, number, inventory, fromInventory, createRange, inputRange);
        command.Parameters.Add("limit", NpgsqlDbType.Integer).Value = pageSize;
        command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = offset;
        var rows = new List<InvProductInLogRow>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) rows.Add(ReadBasis(reader));
        transaction.Commit();
        return new(rows, totalCount, page, pageSize);
    }

    public InvProductInLogDetails Details(int productInType, string? productInNo)
    {
        if (productInType is not (0 or 1)) throw Rule(400, "ProductInType 僅接受 0（FGD）或 1（SEMI）。");
        var number = Required(productInNo, "ProductInNo", 20);
        var table = productInType == 0 ? "tblinvfgdinbasis" : "tblinvsemiinbasis";
        var key = productInType == 0 ? "fgdinno" : "semiinno";
        var toErp = productInType == 0 ? "toerp::int" : "NULL::int";
        var erpFields = productInType == 0
            ? "erpno,canceluser,canceldate"
            : "NULL::varchar(20),NULL::varchar(10),NULL::timestamp without time zone";

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        InvProductInLogRow basis;
        using (var command = new NpgsqlCommand($"SELECT {productInType}::int,{key},state::int,description,creator,createdate,inventoryno,inputdate,source::int,frominventoryno,reviser,revisedate,{toErp},{erpFields},datastamp FROM \"YMES\".{table} WHERE {key}=@number;", connection, transaction))
        {
            AddText(command, "number", number, 20);
            using var reader = command.ExecuteReader();
            if (!reader.Read()) throw Rule(404, "找不到指定的入庫單。");
            basis = ReadBasis(reader);
        }

        var details = new List<InvProductInLogDetailRow>();
        var detailTable = productInType == 0 ? "tblinvfgdindetail" : "tblinvsemiindetail";
        var detailKey = productInType == 0 ? "fgdinno" : "semiinno";
        var detailDates = productInType == 0
            ? "inputdate,frominventoryno"
            : "NULL::timestamp without time zone,NULL::varchar(20)";
        using (var command = new NpgsqlCommand($"""
            SELECT d.productno,p.productname,d.productversion,d.lotno,d.qty,d.unitno,d.locatorno,d.baselotno,{detailDates}
            FROM "YMES".{detailTable} d
            LEFT JOIN LATERAL (
                SELECT productname FROM "YMES".tblprdproductbasis
                WHERE productno=d.productno AND issuestate=2 AND curversion=1
                ORDER BY productversion COLLATE "C" LIMIT 1
            ) p ON true
            WHERE d.{detailKey}=@number
            ORDER BY d.productno COLLATE "C",d.lotno COLLATE "C",d.locatorno COLLATE "C"
            LIMIT 2001;
            """, connection, transaction))
        {
            AddText(command, "number", number, 20);
            using var reader = command.ExecuteReader();
            while (reader.Read()) details.Add(new(reader.GetString(0), NullableText(reader, 1), reader.GetString(2), reader.GetString(3),
                reader.GetDecimal(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), NullableDate(reader, 8), NullableText(reader, 9)));
        }
        if (details.Count > 2000) throw Rule(413, "此單明細超過 2000 筆。");

        var components = new List<InvProductInLogComponentRow>();
        var componentTable = productInType == 0 ? "tblinvfgdincomponent" : "tblinvsemiincomponent";
        var componentKey = productInType == 0 ? "fgdinno" : "semiinno";
        using (var command = new NpgsqlCommand($"SELECT lotno,componentno,goodqty,scrapqty,unitno FROM \"YMES\".{componentTable} WHERE {componentKey}=@number ORDER BY lotno COLLATE \"C\",componentno COLLATE \"C\" LIMIT 5001;", connection, transaction))
        {
            AddText(command, "number", number, 20);
            using var reader = command.ExecuteReader();
            while (reader.Read()) components.Add(new(reader.GetString(0), reader.GetString(1), NullableNumeric(reader, 2),
                NullableNumeric(reader, 3), NullableText(reader, 4)));
        }
        if (components.Count > 5000) throw Rule(413, "此單元件明細超過 5000 筆。");
        transaction.Commit();
        return new(basis, details, components);
    }

    private static InvProductInLogRow ReadBasis(NpgsqlDataReader reader) => new(
        reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2), NullableText(reader, 3), NullableText(reader, 4),
        reader.GetDateTime(5), reader.GetString(6), NullableDate(reader, 7), reader.GetInt32(8), NullableText(reader, 9),
        NullableText(reader, 10), NullableDate(reader, 11), NullableInt(reader, 12), NullableText(reader, 13),
        NullableText(reader, 14), NullableDate(reader, 15), NullableNumeric(reader, 16));

    private static void AddFilters(NpgsqlCommand command, int? type, int? state, string? number, string? inventory,
        string? fromInventory, DateBounds? createRange, DateBounds? inputRange)
    {
        command.Parameters.Add("type", NpgsqlDbType.Integer).Value = (object?)type ?? DBNull.Value;
        command.Parameters.Add("state", NpgsqlDbType.Integer).Value = (object?)state ?? DBNull.Value;
        AddText(command, "number", number, 20);
        AddText(command, "inventory", inventory, 20);
        AddText(command, "from_inventory", fromInventory, 20);
        AddTimestamp(command, "create_from", createRange?.From);
        AddTimestamp(command, "create_to", createRange?.ToExclusive);
        AddTimestamp(command, "input_from", inputRange?.From);
        AddTimestamp(command, "input_to", inputRange?.ToExclusive);
    }

    private static DateBounds? BuildDateRange(DateOnly? from, DateOnly? to, string field)
    {
        if (from.HasValue != to.HasValue) throw Rule(400, $"{field} 起訖日期必須同時提供。");
        if (from is null || to is null) return null;
        if (to.Value < from.Value) throw Rule(400, $"{field} 結束日期不可早於起始日期。");
        if (to.Value == DateOnly.MaxValue) throw Rule(400, $"{field} 結束日期無法使用此日期。");
        if (to.Value.DayNumber - from.Value.DayNumber > 365)
            throw Rule(400, $"{field} 日期範圍最多 366 個日曆日。");
        return new(Timestamp(from.Value), Timestamp(to.Value.AddDays(1)));
    }

    private static DateTime Timestamp(DateOnly date) => DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);

    private static void AddTimestamp(NpgsqlCommand command, string name, DateTime? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Timestamp).Value = (object?)value ?? DBNull.Value;

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page is < 1 or > 100_000) throw Rule(400, "Page 必須介於 1 至 100000。");
        if (pageSize is < 1 or > 100) throw Rule(400, "PageSize 必須介於 1 至 100。");
    }

    private static string? Search(string? value, string field, int length)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > length) throw Rule(400, $"{field} 不可超過 {length} 個字元。");
        return normalized;
    }

    private static string Required(string? value, string field, int length) =>
        Search(value, field, length) ?? throw Rule(400, $"{field} 為必填。");

    private static void AddText(NpgsqlCommand command, string name, string? value, int length) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, length).Value = (object?)value ?? DBNull.Value;

    private static string? NullableText(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static DateTime? NullableDate(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    private static int? NullableInt(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    private static decimal? NullableNumeric(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    private static InvProductInRuleException Rule(int statusCode, string message) => new(statusCode, message);

    private sealed record DateBounds(DateTime From, DateTime ToExclusive);
}
