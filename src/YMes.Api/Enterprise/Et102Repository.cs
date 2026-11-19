using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Enterprise;

public sealed class Et102Repository(NpgsqlDataSource dataSource)
{
    private const decimal MaxStamp = 99_999_999_999m;
    private const string Table = "\"YMES\".tblentcustomercontactor";
    private const string Columns = "contactorname,customerno,telno,faxno,email,title,deptname,creator,createdate,description,datastamp,reviser,revisedate";
    private const string LogicalOwner = "lower(btrim(customerno))=lower(btrim(@customer))";
    private const string LogicalName = "lower(btrim(contactorname))=lower(btrim(@name))";
    private const string ExactKey = "customerno=@customer AND contactorname=@name";

    public IReadOnlyList<Et102CustomerOption> Customers()
    {
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("""
            SELECT customerno,customername,customersname,issuestate::int,
              count(*) OVER(PARTITION BY lower(btrim(customerno)))
            FROM "YMES".tblentcustomerbasis
            ORDER BY lower(btrim(customerno)) COLLATE "C",customerno COLLATE "C";
            """, connection);
        using var reader = command.ExecuteReader(); var rows = new List<Et102CustomerOption>();
        while (reader.Read())
        {
            if (reader.GetInt64(4) > 1) throw Rule(409, "客戶編號存在大小寫／空白歧義，不能任選來源列。");
            rows.Add(MapCustomer(reader));
        }
        // ET102 is explicitly all-state, including NULL/unknown states. No '*'.
        return rows;
    }

    public IReadOnlyList<Et102ContactorDto> List(string customerNo)
    {
        var no = Key(customerNo, "CustomerNo", 50);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var customer = ReadCustomer(connection, transaction, no);
        using var command = new NpgsqlCommand($"""
            SELECT {Columns},count(*) OVER(PARTITION BY lower(btrim(contactorname)))
            FROM {Table} WHERE {LogicalOwner}
            ORDER BY contactorname COLLATE "C",customerno COLLATE "C";
            """, connection, transaction);
        Text(command, "customer", customer.CustomerNo);
        var rows = new List<Et102ContactorDto>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                if (reader.GetInt64(13) > 1) throw Rule(409, "此客戶的連絡人複合鍵存在大小寫／空白歧義，原列已保留。");
                rows.Add(Map(reader));
            }
        }
        transaction.Commit(); return rows;
    }

    public Et102ContactorDto Get(string customerNo, string contactorName)
    {
        var no = Key(customerNo, "CustomerNo", 50); var name = Key(contactorName, "ContactorName", 50);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var customer = ReadCustomer(connection, transaction, no);
        var row = ReadContactor(connection, transaction, customer.CustomerNo, name) ?? throw Missing();
        transaction.Commit(); return row;
    }

    public Et102ContactorDto Create(string customerNo, Et102ContactorWriteRequest request, string actor)
    {
        var no = Key(customerNo, "CustomerNo", 50); var name = Key(request.ContactorName, "ContactorName", 50);
        actor = Actor(actor); ValidateFields(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var customer = LockCustomer(connection, transaction, no);
        AcquireContactor(connection, transaction, customer.CustomerNo, name);
        if (ReadContactor(connection, transaction, customer.CustomerNo, name) is not null)
            throw Rule(409, "此客戶已存在相同連絡人，不能接管或覆寫來源列。");
        using var command = new NpgsqlCommand($"""
            INSERT INTO {Table}
              (contactorname,customerno,telno,faxno,email,title,deptname,creator,createdate,description,datastamp,reviser,revisedate)
            VALUES(@name,@customer,@tel,@fax,@email,@title,@dept,@actor,now() AT TIME ZONE 'UTC',@description,0,NULL,NULL)
            RETURNING {Columns};
            """, connection, transaction);
        Bind(command, customer.CustomerNo, name, request, actor);
        var result = Returned(command); transaction.Commit(); return result;
    }

    public Et102ContactorDto Update(string customerNo, string contactorName, Et102ContactorUpdateRequest request, string actor)
    {
        var no = Key(customerNo, "CustomerNo", 50); var name = Key(contactorName, "ContactorName", 50);
        actor = Actor(actor); ValidateStamp(request.DataStamp); ValidateFields(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var customer = LockCustomer(connection, transaction, no);
        AcquireContactor(connection, transaction, customer.CustomerNo, name);
        var current = ReadContactor(connection, transaction, customer.CustomerNo, name, update: true) ?? throw Missing();
        Expected(current.DataStamp, request.DataStamp); Incrementable(current.DataStamp);
        if (!string.Equals(request.ContactorName, current.ContactorName, StringComparison.Ordinal))
            throw Rule(400, "修改時不可變更連絡人姓名，請保留原始主鍵值。");
        using var command = new NpgsqlCommand($"""
            UPDATE {Table}
            SET telno=@tel,faxno=@fax,email=@email,title=@title,deptname=@dept,description=@description,
                reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE {ExactKey} AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {Columns};
            """, connection, transaction);
        // Keep native owner/name and creation audit exactly, even when padding
        // differs from the Customer lookup. Only editable business fields change.
        Bind(command, current.CustomerNo, current.ContactorName, request, actor); Number(command, "stamp", request.DataStamp);
        var result = Returned(command); transaction.Commit(); return result;
    }

    public void Delete(string customerNo, string contactorName, Et102ExpectedRequest request, string actor)
    {
        var no = Key(customerNo, "CustomerNo", 50); var name = Key(contactorName, "ContactorName", 50);
        _ = Actor(actor); ValidateStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var customer = LockCustomer(connection, transaction, no);
        AcquireContactor(connection, transaction, customer.CustomerNo, name);
        var current = ReadContactor(connection, transaction, customer.CustomerNo, name, update: true) ?? throw Missing();
        Expected(current.DataStamp, request.DataStamp);
        // Delete does not increment; a matching maximum native token is valid.
        using var command = new NpgsqlCommand($"DELETE FROM {Table} WHERE {ExactKey} AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction);
        Text(command, "customer", current.CustomerNo); Text(command, "name", current.ContactorName); Number(command, "stamp", request.DataStamp);
        if (command.ExecuteNonQuery() != 1) throw Conflict();
        transaction.Commit();
    }

    private static Et102CustomerOption LockCustomer(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        // Shared identity with a future ET101 writer; no ET101 endpoint is added
        // by this candidate. Parent existence/state are checked after waiting.
        using (var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:ENT:Customer:' || lower(btrim(@customer)),0));", connection, transaction))
        { Text(command, "customer", no); command.ExecuteNonQuery(); }
        return ReadCustomer(connection, transaction, no);
    }

    private static Et102CustomerOption ReadCustomer(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("""
            SELECT customerno,customername,customersname,issuestate::int
            FROM "YMES".tblentcustomerbasis WHERE lower(btrim(customerno))=lower(btrim(@customer))
            ORDER BY customerno COLLATE "C" FOR SHARE;
            """, connection, transaction);
        Text(command, "customer", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(422, "客戶不存在，連絡人資料未變更；請重新選擇真實客戶。");
        var row = MapCustomer(reader);
        if (reader.Read()) throw Rule(409, "客戶編號存在大小寫／空白歧義，不能任選來源列。");
        return row; // All states, including NULL, are accepted by source ET102.
    }

    private static void AcquireContactor(NpgsqlConnection connection, NpgsqlTransaction transaction, string customer, string name)
    {
        using var command = new NpgsqlCommand("""
            SELECT pg_advisory_xact_lock(hashtextextended('YMES:ENT:Contactor:' ||
              jsonb_build_array(lower(btrim(@customer)),lower(btrim(@name)))::text,0));
            """, connection, transaction);
        Text(command, "customer", customer); Text(command, "name", name); command.ExecuteNonQuery();
    }

    private static Et102ContactorDto? ReadContactor(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string customer, string name, bool update = false)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM {Table} WHERE {LogicalOwner} AND {LogicalName} ORDER BY customerno COLLATE \"C\",contactorname COLLATE \"C\"" + (update ? " FOR UPDATE;" : ";"), connection, transaction);
        Text(command, "customer", customer); Text(command, "name", name); using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var row = Map(reader);
        if (reader.Read()) throw Rule(409, "連絡人複合鍵存在大小寫／空白歧義，原列已保留。");
        return row;
    }

    private static void ValidateFields(Et102ContactorWriteRequest request)
    {
        Optional(request.TelNo, "TelNo", 25); Optional(request.FaxNo, "FaxNo", 25);
        Optional(request.Email, "Email", 50); Optional(request.Title, "Title", 25);
        Optional(request.DeptName, "DeptName", 25); Optional(request.Description, "Description", 255, multiline: true);
    }
    private static string Key(string? value, string label, int length)
    {
        var result = value?.Trim(' ') ?? "";
        if (string.IsNullOrWhiteSpace(result) || result.Length > length || result.Any(char.IsControl))
            throw Rule(400, $"{label} 必填、最多{length}字元且不可含控制字元。");
        return result;
    }
    private static void Optional(string? value, string label, int length, bool multiline = false)
    {
        if (value?.Length > length || value?.Any(c => char.IsControl(c) && !(multiline && (c is '\r' or '\n' or '\t'))) == true)
            throw Rule(400, $"{label} 最多{length}字元且不可含無效控制字元。");
    }
    private static string Actor(string value) => Key(value, "登入使用者", 10);
    private static void ValidateStamp(decimal? value)
    { if (value is { } stamp && (stamp != decimal.Truncate(stamp) || stamp < -MaxStamp || stamp > MaxStamp)) throw Rule(400, "DataStamp 必須明確為NULL或numeric(11,0)範圍內整數。"); }
    private static void Expected(decimal? actual, decimal? expected)
    { if (actual != expected) throw Conflict(); }
    private static void Incrementable(decimal? value)
    { ValidateStamp(value); if (value == MaxStamp) throw Rule(409, "DataStamp 已達來源上限，資料未變更。"); }
    private static string? String(NpgsqlDataReader reader, int i) => reader.IsDBNull(i) ? null : reader.GetString(i);
    private static int? Int(NpgsqlDataReader reader, int i) => reader.IsDBNull(i) ? null : reader.GetInt32(i);
    private static DateTime? Date(NpgsqlDataReader reader, int i) => reader.IsDBNull(i) ? null : DateTime.SpecifyKind(reader.GetDateTime(i), DateTimeKind.Utc);
    private static Et102CustomerOption MapCustomer(NpgsqlDataReader r) => new(r.GetString(0), String(r, 1), String(r, 2), Int(r, 3));
    private static Et102ContactorDto Map(NpgsqlDataReader r) => new(r.GetString(0), r.GetString(1), String(r, 2), String(r, 3), String(r, 4), String(r, 5), String(r, 6), String(r, 7), Date(r, 8), String(r, 9), r.IsDBNull(10) ? null : r.GetDecimal(10), String(r, 11), Date(r, 12));
    private static Et102ContactorDto Returned(NpgsqlCommand command)
    { using var reader = command.ExecuteReader(); if (!reader.Read()) throw Conflict(); var row = Map(reader); if (reader.Read()) throw Conflict(); return row; }
    private static void Bind(NpgsqlCommand command, string customer, string name, Et102ContactorWriteRequest request, string actor)
    {
        Text(command, "customer", customer); Text(command, "name", name); Text(command, "tel", request.TelNo);
        Text(command, "fax", request.FaxNo); Text(command, "email", request.Email); Text(command, "title", request.Title);
        Text(command, "dept", request.DeptName); Text(command, "description", request.Description); Text(command, "actor", actor);
    }
    private static void Text(NpgsqlCommand command, string name, string? value) => command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand command, string name, decimal? value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static Et102RuleException Rule(int code, string message) => new(code, message);
    private static Et102RuleException Missing() => Rule(404, "找不到指定客戶連絡人。");
    private static Et102RuleException Conflict() => Rule(409, "連絡人已被其他操作更新，請重新查詢；本次未保存。");
}
