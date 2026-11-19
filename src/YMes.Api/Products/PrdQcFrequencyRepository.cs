using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Products;

public sealed class PrdQcFrequencyRepository(NpgsqlDataSource dataSource)
{
    private const string Table = "tblprdqcrulebasis";
    private const decimal MaxStamp = 99999999999m;
    private const string Columns = "productno,equipmentno,opno,qcmethod,qcnum,shifta,shiftb,description,creator,createdate,qcitem,qctype,moduleno,datastamp,reviser,revisedate";
    private const string Match = "lower(productno)=lower(@product) AND lower(equipmentno)=lower(@equipment) AND lower(opno)=lower(@op)";
    private sealed record Key(string Product, string Equipment, string Op);
    private sealed record Value(int Method, int? Count, string? A, string? B, string? Description);

    public IReadOnlyList<PrdQcFrequencyDto> List(string? search)
    {
        var term = TextValue(search, "查詢值", 255);
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, Table);
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".{Table} " +
            "WHERE @search IS NULL OR strpos(lower(productno),lower(@search))>0 OR strpos(lower(equipmentno),lower(@search))>0 " +
            "OR strpos(lower(opno),lower(@search))>0 OR strpos(lower(COALESCE(description,'')),lower(@search))>0 ORDER BY productno,equipmentno,opno;", connection);
        Text(command, "search", term, 255);
        var rows = new List<PrdQcFrequencyDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) rows.Add(Read(reader));
        return rows;
    }

    public PrdQcFrequencyDto Get(PrdQcFrequencyKeyRequest request)
    {
        var key = ReadKey(request);
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, Table);
        return Find(connection, null, key, false) ?? throw Rule(404, "找不到這組抽樣頻率規則。");
    }

    public IReadOnlyList<PrdQcFrequencyOption> Options(string kind)
    {
        var (table, sql) = kind switch
        {
            "products" => ("tblprdproductbasis", "SELECT productno,min(productname) FROM \"YMES\".tblprdproductbasis WHERE issuestate=2 GROUP BY productno ORDER BY productno;"),
            "equipment" => ("tbleqpequipmentbasis", "SELECT equipmentno,equipmentname FROM \"YMES\".tbleqpequipmentbasis ORDER BY equipmentno;"),
            "operations" => ("tblopbasis", "SELECT opno,opname FROM \"YMES\".tblopbasis ORDER BY opno;"),
            _ => throw Rule(400, "不支援的查找類型。")
        };
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, table);
        using var command = new NpgsqlCommand(sql, connection);
        var values = new List<PrdQcFrequencyOption>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) values.Add(new(reader.GetString(0), NullableText(reader, 1)));
        return values;
    }

    public PrdQcFrequencyDto Create(PrdQcFrequencyWriteRequest request, string actor)
    {
        var key = ReadKey(request); var value = Validate(request); ValidateActor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, Table); LockIdentifier(connection, transaction, key);
            key = References(connection, transaction, key);
            if (Find(connection, transaction, key, true) is not null) throw Rule(409, "這組產品、設備與作業站已存在規則。");
            // No source DEFAULT exists for DataStamp. A new row therefore retains NULL.
            using var command = new NpgsqlCommand($"""
                INSERT INTO "YMES".tblprdqcrulebasis
                    (productno,equipmentno,opno,qcmethod,qcnum,shifta,shiftb,description,moduleno,creator,createdate)
                VALUES(@product,@equipment,@op,@method,@count,@a,@b,@description,'WIP',@actor,(now() AT TIME ZONE 'UTC'))
                RETURNING {Columns};
                """, connection, transaction);
            BindKey(command, key); BindValue(command, value); Text(command, "actor", actor, 10);
            var result = Returned(command); transaction.Commit(); return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public PrdQcFrequencyDto Update(PrdQcFrequencyUpdateRequest request, string actor)
    {
        var key = ReadKey(request); var value = Validate(request); ValidateStamp(request.DataStamp); ValidateActor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, Table); LockIdentifier(connection, transaction, key);
            // All writers take identifier, product/equipment/operation parents, then the rule row.
            References(connection, transaction, key);
            var current = Find(connection, transaction, key, true) ?? throw Rule(404, "找不到這組抽樣頻率規則。");
            Expected(current, request.DataStamp);
            if (current.DataStamp.HasValue && current.DataStamp.Value >= MaxStamp) throw Rule(409, "資料版本已達來源 11 位數上限，請由維護人員處理；資料未變更。");
            using var command = new NpgsqlCommand($"""
                UPDATE "YMES".tblprdqcrulebasis SET qcmethod=@method,qcnum=@count,shifta=@a,shiftb=@b,
                    description=@description,reviser=@actor,revisedate=(now() AT TIME ZONE 'UTC'),datastamp=COALESCE(datastamp,0)+1
                WHERE productno=@product AND equipmentno=@equipment AND opno=@op AND datastamp IS NOT DISTINCT FROM @stamp
                RETURNING {Columns};
                """, connection, transaction);
            BindKey(command, new(current.ProductNo, current.EquipmentNo, current.OpNo)); BindValue(command, value);
            Number(command, "stamp", request.DataStamp); Text(command, "actor", actor, 10);
            var result = Returned(command); transaction.Commit(); return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public PrdQcFrequencyDto Delete(PrdQcFrequencyDeleteRequest request, string actor)
    {
        var key = ReadKey(request); ValidateStamp(request.DataStamp); ValidateActor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, Table); LockIdentifier(connection, transaction, key);
            var current = Find(connection, transaction, key, true) ?? throw Rule(404, "找不到這組抽樣頻率規則。");
            Expected(current, request.DataStamp);
            // No parent lookup is required to remove an existing orphan association.
            using var command = new NpgsqlCommand("DELETE FROM \"YMES\".tblprdqcrulebasis WHERE productno=@product AND equipmentno=@equipment AND opno=@op AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction);
            BindKey(command, new(current.ProductNo, current.EquipmentNo, current.OpNo)); Number(command, "stamp", request.DataStamp);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
            transaction.Commit(); return current;
        }
        catch { Rollback(transaction); throw; }
    }

    private static Key References(NpgsqlConnection connection, NpgsqlTransaction transaction, Key key) => new(
        Parent(connection, transaction, "tblprdproductbasis", "productno", key.Product, true),
        Parent(connection, transaction, "tbleqpequipmentbasis", "equipmentno", key.Equipment, false),
        Parent(connection, transaction, "tblopbasis", "opno", key.Op, false));

    private static string Parent(NpgsqlConnection connection, NpgsqlTransaction transaction, string table, string column, string key, bool product)
    {
        if (key == "*") return key;
        EnsureTable(connection, transaction, table);
        // SQL identifiers here are fixed internal allowlist values, never request fields.
        using var command = new NpgsqlCommand($"SELECT {column},issuestate FROM \"YMES\".{table} WHERE lower({column})=lower(@key) ORDER BY {column}" +
            (product ? ",productversion" : "") + " FOR SHARE;", connection, transaction);
        Text(command, "key", key, product ? 50 : column == "opno" ? 30 : 50);
        var names = new HashSet<string>(StringComparer.Ordinal); var approved = false;
        using var reader = command.ExecuteReader();
        while (reader.Read()) { names.Add(reader.GetString(0)); if (!reader.IsDBNull(1) && reader.GetInt32(1) == 2) approved = true; }
        if (names.Count > 1) throw Rule(409, "參照主檔存在大小寫不同的同名編號，請先整理主檔。");
        if (names.Count == 0 || product && !approved) throw Rule(422, product ? $"產品 {key} 不存在或沒有已核准版次。" : $"找不到參照主檔 {key}。");
        var canonical = names.Single();
        if (canonical.Length > (product ? 25 : column == "opno" ? 30 : 50)) throw Rule(400, "參照編號超過抽樣頻率來源欄位長度，不能截斷保存。");
        return canonical;
    }

    private static void LockIdentifier(NpgsqlConnection connection, NpgsqlTransaction transaction, Key key)
    {
        using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:PD206:'||json_build_array(lower(@product),lower(@equipment),lower(@op))::text,0));", connection, transaction);
        BindKey(command, key); command.ExecuteNonQuery();
    }
    private static PrdQcFrequencyDto? Find(NpgsqlConnection connection, NpgsqlTransaction? transaction, Key key, bool update)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".{Table} WHERE {Match} ORDER BY productno,equipmentno,opno" + (update ? " FOR UPDATE;" : ";"), connection, transaction);
        BindKey(command, key);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var row = Read(reader);
        if (reader.Read()) throw Rule(409, "抽樣頻率存在大小寫不同的相同組合，請先整理資料。");
        return row;
    }
    private static Value Validate(PrdQcFrequencyWriteRequest request)
    {
        if (request.QcMethod is not (0 or 1 or 2)) throw Rule(400, "頻率只允許多批、每天第一批或每班第一批。");
        if (request.QcMethod == 0 && request.QcNum is not (>= 1 and <= 99)) throw Rule(400, "抽樣批次數須為 1 到 99 的整數。");
        var a = TextValue(request.ShiftA, "A 班時分", 5); var b = TextValue(request.ShiftB, "B 班時分", 5);
        if (request.QcMethod != 0 && !ValidShift(a) || request.QcMethod == 2 && !ValidShift(b)) throw Rule(400, "請填寫必要班別時分，格式 HH:mm，時 00–24、分 00–59。");
        return new(request.QcMethod.Value, request.QcMethod == 0 ? request.QcNum : null,
            request.QcMethod == 0 ? null : a, request.QcMethod == 2 ? b : null, TextValue(request.Description, "說明", 255));
    }
    private static bool ValidShift(string? value) => value is not null && Regex.IsMatch(value, "^(?:[01][0-9]|2[0-4]):[0-5][0-9]$", RegexOptions.CultureInvariant);
    private static Key ReadKey(PrdQcFrequencyKeyRequest request) => new(Required(request.ProductNo, "產品", 25), Required(request.EquipmentNo, "設備", 50), Required(request.OpNo, "作業站", 30));
    private static string Required(string? value, string field, int length) => TextValue(value, field, length) ?? throw Rule(400, $"{field}編號為必填；全部請明確傳入 *。");
    private static string? TextValue(string? value, string field, int length)
    { var text = value?.Trim(); if (text?.Length > length || text?.Any(char.IsControl) == true) throw Rule(400, $"{field}不可超過 {length} 個字元或含控制字元。"); return string.IsNullOrEmpty(text) ? null : text; }
    private static void ValidateActor(string actor) { if (string.IsNullOrWhiteSpace(actor) || actor.Length > 10) throw Rule(400, "登入帳號不符合來源稽核欄位，請重新登入。"); }
    private static void ValidateStamp(decimal? stamp) { if (stamp.HasValue && (decimal.Truncate(stamp.Value) != stamp.Value || stamp.Value < -MaxStamp || stamp.Value > MaxStamp)) throw Rule(400, "DataStamp 須為明確 NULL 或 11 位數範圍內整數。"); }
    private static void Expected(PrdQcFrequencyDto current, decimal? stamp) { if (current.DataStamp != stamp) throw Conflict(); }
    private static PrdQcFrequencyDto Returned(NpgsqlCommand command) { using var reader = command.ExecuteReader(); return reader.Read() ? Read(reader) : throw Conflict(); }
    private static PrdQcFrequencyDto Read(NpgsqlDataReader r) => new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetInt32(3),r.IsDBNull(4)?null:r.GetInt32(4),NullableText(r,5),NullableText(r,6),NullableText(r,7),NullableText(r,8),Date(r,9),NullableText(r,10),r.IsDBNull(11)?null:r.GetInt32(11),NullableText(r,12),r.IsDBNull(13)?null:r.GetDecimal(13),NullableText(r,14),Date(r,15));
    private static string? NullableText(NpgsqlDataReader r, int i) => r.IsDBNull(i)?null:r.GetString(i);
    private static DateTime? Date(NpgsqlDataReader r, int i) => r.IsDBNull(i)?null:DateTime.SpecifyKind(r.GetDateTime(i),DateTimeKind.Utc);
    private static void BindKey(NpgsqlCommand c, Key key) { Text(c,"product",key.Product,25);Text(c,"equipment",key.Equipment,50);Text(c,"op",key.Op,30); }
    private static void BindValue(NpgsqlCommand c, Value v) { Number(c,"method",v.Method);Number(c,"count",v.Count);Text(c,"a",v.A,5);Text(c,"b",v.B,5);Text(c,"description",v.Description,255); }
    private static void Text(NpgsqlCommand c, string name, string? value, int size) => c.Parameters.Add(name,NpgsqlDbType.Varchar,size).Value=(object?)value??DBNull.Value;
    private static void Number(NpgsqlCommand c, string name, decimal? value) => c.Parameters.Add(name,NpgsqlDbType.Numeric).Value=(object?)value??DBNull.Value;
    private static void EnsureTable(NpgsqlConnection c, NpgsqlTransaction? t, string table)
    { using var q=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@table AND c.relkind IN ('r','p'));",c,t);Text(q,"table",table,63);if(q.ExecuteScalar() is not true)throw Rule(503,$"YMES 尚未建立 {table}，請先套用已核實的 migration。"); }
    private static void Rollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { /* retain operation failure */ } }
    private static PrdQcFrequencyRuleException Rule(int status, string message) => new(status,message);
    private static PrdQcFrequencyRuleException Conflict() => Rule(409,"抽樣頻率已被其他作業修改，請重新查詢後再操作。");
}
