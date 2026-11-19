using System.Data;
using MES.Api.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Products;

public sealed class PrdProductTypeRepository(NpgsqlDataSource dataSource)
{
    private const string Columns = "producttype,description,creator,createdate,issuestate::int,ex_prdtype1,ex_prdtype2,producttypecode,datastamp,reviser,revisedate";
    private const decimal MaxStamp = 99999999999m;

    public IReadOnlyList<PrdProductTypeDto> List(string? search, int? issueState)
    {
        var term = Optional(search, "查詢值", 50);
        if (issueState.HasValue && issueState is not (-1 or 0 or 1 or 2)) throw Rule(400, "查詢狀態無效。");
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblprdtype WHERE (@state IS NULL OR issuestate=@state) AND (@term IS NULL OR strpos(lower(producttype),lower(@term))>0) ORDER BY producttype;", connection);
        Text(command, "term", term, 50); Number(command, "state", issueState);
        using var reader = command.ExecuteReader();
        var rows = new List<PrdProductTypeDto>();
        while (reader.Read()) rows.Add(Read(reader));
        return rows;
    }

    public PrdProductTypeDto Get(string productType)
    {
        using var connection = dataSource.OpenConnection();
        return Find(connection, null, PrdProductTypeReference.Key(productType), false);
    }

    public PrdProductTypeDto Create(PrdProductTypeWriteRequest request, string actor)
    {
        var no = PrdProductTypeReference.Key(request.ProductType).ToUpperInvariant();
        var code = Optional(request.ProductTypeCode, "產品類別代碼", 10);
        var description = Optional(request.Description, "說明", 255, multiline: true);
        actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        PrdProductTypeReference.LockCreationNamespace(connection, transaction, no);
        PrdProductTypeReference.EnsureNewTypeNamespace(connection, transaction, no);
        using (var duplicate = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblprdtype WHERE lower(btrim(producttype))=lower(@type));", connection, transaction))
        {
            Text(duplicate, "type", no, 50);
            if ((bool)(duplicate.ExecuteScalar() ?? false)) throw Rule(409, "產品類別編號已存在。");
        }
        using (var command = new NpgsqlCommand("""
            INSERT INTO "YMES".tblprdtype(producttype,producttypecode,description,creator,createdate,issuestate)
            VALUES(@type,@code,@description,@actor,(now() AT TIME ZONE 'UTC'),0);
            """, connection, transaction))
        {
            Text(command, "type", no, 50); Text(command, "code", code, 10);
            Text(command, "description", description, 255); Text(command, "actor", actor, 10);
            command.ExecuteNonQuery();
        }
        var result = Find(connection, transaction, no, false);
        transaction.Commit(); return result;
    }

    public PrdProductTypeDto Update(string productType, PrdProductTypeUpdateRequest request, string actor)
    {
        var no = PrdProductTypeReference.Key(productType);
        if (!string.Equals(no, PrdProductTypeReference.Key(request.ProductType), StringComparison.OrdinalIgnoreCase))
            throw Rule(400, "修改時不可變更產品類別編號。");
        var code = Optional(request.ProductTypeCode, "產品類別代碼", 10);
        var description = Optional(request.Description, "說明", 255, multiline: true);
        actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var current = Find(connection, transaction, no, true);
        Expected(current, request.IssueState, request.DataStamp);
        if (current.IssueState != 0) throw Rule(409, "只有未核准的產品類別可以修改。");
        CanAdvance(current.DataStamp);
        using (var command = new NpgsqlCommand("""
            UPDATE "YMES".tblprdtype SET producttypecode=@code,description=@description,
              reviser=@actor,revisedate=(now() AT TIME ZONE 'UTC'),datastamp=COALESCE(datastamp,0)+1
            WHERE producttype=@type AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp;
            """, connection, transaction))
        {
            Text(command, "code", code, 10); Text(command, "description", description, 255);
            Bind(command, current, actor); Changed(command);
        }
        var result = Find(connection, transaction, current.ProductType, false);
        transaction.Commit(); return result;
    }

    public PrdProductTypeDto Approve(string productType, PrdProductTypeCommandRequest request, string actor)
    {
        var no = PrdProductTypeReference.Key(productType);
        actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var current = Find(connection, transaction, no, true);
        Expected(current, request.IssueState, request.DataStamp);
        if (current.IssueState != 0) throw Rule(409, "只有未核准的產品類別可以核准。");
        CanAdvance(current.DataStamp);
        foreach (var table in new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" })
        {
            using var job = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@table AND c.relkind IN ('r','p'));", connection, transaction);
            Text(job, "table", table, 63);
            if ((bool)(job.ExecuteScalar() ?? false)) throw Rule(503, "產品類別尚未接入現存 Issue/Job 簽核服務，資料未變更。");
        }
        using (var command = new NpgsqlCommand("""
            UPDATE "YMES".tblprdtype SET issuestate=2,reviser=@actor,revisedate=(now() AT TIME ZONE 'UTC'),datastamp=COALESCE(datastamp,0)+1
            WHERE producttype=@type AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp;
            """, connection, transaction))
        { Bind(command, current, actor); Changed(command); }
        var result = Find(connection, transaction, current.ProductType, false);
        transaction.Commit(); return result;
    }

    public PrdProductTypeDeleteResult Delete(string productType, PrdProductTypeCommandRequest request, string actor)
    {
        var no = PrdProductTypeReference.Key(productType);
        actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var current = Find(connection, transaction, no, true);
        Expected(current, request.IssueState, request.DataStamp);
        if (current.IssueState == 2)
        {
            CanAdvance(current.DataStamp);
            using (var command = new NpgsqlCommand("""
                UPDATE "YMES".tblprdtype SET issuestate=-1,reviser=@actor,revisedate=(now() AT TIME ZONE 'UTC'),datastamp=COALESCE(datastamp,0)+1
                WHERE producttype=@type AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp;
                """, connection, transaction))
            { Bind(command, current, actor); Changed(command); }
            var result = Find(connection, transaction, current.ProductType, false);
            transaction.Commit(); return new("UNUSED", result);
        }
        if (current.IssueState is not (0 or -1)) throw Rule(409, "只有未核准或不使用的類別可以實體刪除；簽核中不可刪除。");
        EnsureNoReferences(connection, transaction, current.ProductType);
        using (var command = new NpgsqlCommand("DELETE FROM \"YMES\".tblprdtype WHERE producttype=@type AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction))
        { Bind(command, current, actor); Changed(command); }
        transaction.Commit(); return new("DELETED", current);
    }

    private static void EnsureNoReferences(NpgsqlConnection connection, NpgsqlTransaction transaction, string productType)
    {
        // The parent row is already locked. Product writers take a SHARE lock on
        // this same type. Do not take a reverse Product-row lock here.
        using (var used = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM \"YMES\".tblprdproductbasis WHERE lower(btrim(producttype))=lower(btrim(@type)));", connection, transaction))
        {
            Text(used, "type", productType, 50);
            if ((bool)(used.ExecuteScalar() ?? false)) throw Rule(409, "產品仍使用此類別，不能實體刪除。");
        }
        // Source shares ProductNo + Version=-1 across several product settings.
        // Until complete source-specific cleanup is implemented, preserve any
        // existing sentinel rows. Catalog inspection is not privilege filtered.
        var tables = new List<string>();
        using (var catalog = new NpgsqlCommand("""
            SELECT c.relname FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='YMES' AND c.relkind IN ('r','p')
              AND (starts_with(c.relname,'tblprd') OR starts_with(c.relname,'ym_product'))
              AND EXISTS(SELECT 1 FROM pg_catalog.pg_attribute a WHERE a.attrelid=c.oid AND a.attname='productno' AND a.attnum>0 AND NOT a.attisdropped)
              AND EXISTS(SELECT 1 FROM pg_catalog.pg_attribute a WHERE a.attrelid=c.oid AND a.attname='productversion' AND a.attnum>0 AND NOT a.attisdropped)
            ORDER BY c.relname;
            """, connection, transaction))
        {
            using var reader = catalog.ExecuteReader();
            while (reader.Read()) tables.Add(reader.GetString(0));
        }
        using var quoting = new NpgsqlCommandBuilder();
        foreach (var table in tables)
        {
            var quoted = quoting.QuoteIdentifier(table);
            using var used = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM \"YMES\".{quoted} WHERE lower(btrim(productno::text))=lower(btrim(@type)) AND productversion::text='-1');", connection, transaction);
            Text(used, "type", productType, 50);
            if ((bool)(used.ExecuteScalar() ?? false)) throw Rule(409, "此類別仍有共用版本 -1 設定，尚未接入完整設定清理，資料已保留。");
        }
    }

    private static PrdProductTypeDto Find(NpgsqlConnection connection, NpgsqlTransaction? transaction, string no, bool forUpdate)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblprdtype WHERE lower(btrim(producttype))=lower(@type) ORDER BY producttype" + (forUpdate ? " FOR UPDATE;" : ";"), connection, transaction);
        Text(command, "type", no, 50);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到指定產品類別。");
        var result = Read(reader);
        if (reader.Read()) throw Rule(409, "產品類別存在大小寫或空白重複，請先整理主檔。");
        return result;
    }

    private static PrdProductTypeDto Read(NpgsqlDataReader reader) => new(reader.GetString(0), String(reader, 1),
        String(reader, 2), Date(reader, 3), reader.GetInt32(4), String(reader, 5), String(reader, 6),
        String(reader, 7), reader.IsDBNull(8) ? null : reader.GetDecimal(8), String(reader, 9), Date(reader, 10));
    private static string? String(NpgsqlDataReader reader, int i) => reader.IsDBNull(i) ? null : reader.GetString(i);
    private static DateTime? Date(NpgsqlDataReader reader, int i) => reader.IsDBNull(i) ? null : DateTime.SpecifyKind(reader.GetDateTime(i), DateTimeKind.Utc);
    private static void Expected(PrdProductTypeDto current, int? state, decimal? stamp)
    {
        if (state is not (-1 or 0 or 1 or 2)) throw Rule(400, "必須明確提供有效 IssueState。");
        if (stamp.HasValue && (stamp < -MaxStamp || stamp > MaxStamp || stamp != decimal.Truncate(stamp.Value)))
            throw Rule(400, "DataStamp 必須為資料表範圍內的整數或 NULL。");
        if (current.IssueState != state || current.DataStamp != stamp) throw Rule(409, "資料已被其他操作變更，請重新查詢。");
    }
    private static void CanAdvance(decimal? stamp)
    { if (stamp >= MaxStamp) throw Rule(409, "DataStamp 已達上限，無法再修改狀態或內容。"); }
    private static string Actor(string? actor)
    {
        var value = actor?.Trim() ?? "";
        if (value.Length is < 1 or > 10 || value.Any(char.IsControl)) throw Rule(400, "登入帳號不符合稽核欄位長度。");
        return value;
    }
    private static string? Optional(string? value, string label, int maximum, bool multiline = false)
    {
        var text = value?.Trim();
        if (text?.Length > maximum || text?.Any(c => char.IsControl(c) && !(multiline && (c is '\r' or '\n' or '\t'))) == true)
            throw Rule(400, $"{label}最多 {maximum} 個字元，且不可含非文字控制字元。");
        return string.IsNullOrEmpty(text) ? null : text;
    }
    private static void Bind(NpgsqlCommand command, PrdProductTypeDto current, string actor)
    { Text(command, "type", current.ProductType, 50); Text(command, "actor", actor, 10); Number(command, "state", current.IssueState); Number(command, "stamp", current.DataStamp); }
    private static void Changed(NpgsqlCommand command)
    { if (command.ExecuteNonQuery() != 1) throw Rule(409, "資料已被其他操作變更，請重新查詢。"); }
    private static void Text(NpgsqlCommand command, string name, string? value, int size) => PrdProductTypeReference.Text(command, name, value, size);
    private static void Number(NpgsqlCommand command, string name, decimal? value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static MasterDataRuleException Rule(int code, string message) => new(code, message);
}
