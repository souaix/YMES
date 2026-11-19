using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Operations;

public sealed class OpB03Repository(NpgsqlDataSource dataSource)
{
    private const decimal MaxStamp = 99_999_999_999m;
    private const string HeaderTable = "\"YMES\".tbl_bopcustopmapbasis";
    private const string DetailTable = "\"YMES\".tbl_bopcustopmapdetail";
    private const string HeaderColumns = "customerno,customeropno,customeropname,customeropseq::int,psno,description,creator,createdate,datastamp,reviser,revisedate";
    private const string DetailColumns = "customerno,customeropno,psno,opno,custopprocesscode,custstepname,transactionflag::int,creator,createdate,datastamp,reviser,revisedate";
    private const string OwnerMatch = "lower(btrim(customerno))=lower(btrim(@customer)) AND lower(btrim(customeropno))=lower(btrim(@mapping))";
    private const string ExactOwner = "customerno=@customer AND customeropno=@mapping";

    public IReadOnlyList<OpB03HeaderDto> List(string? customerNo, string? customerOPNo)
    {
        var customer = Search(customerNo, "CustomerNo", 50);
        var mapping = Search(customerOPNo, "CustomerOPNo", 20);
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand($"""
            WITH headers AS (
              SELECT *,count(*) OVER(PARTITION BY lower(btrim(customerno)),lower(btrim(customeropno))) AS aliases
              FROM {HeaderTable})
            SELECT {HeaderColumns},aliases FROM headers
            WHERE (@customer IS NULL OR position(lower(@customer) in lower(customerno))>0)
              AND (@mapping IS NULL OR position(lower(@mapping) in lower(customeropno))>0)
            ORDER BY lower(btrim(customerno)) COLLATE "C",lower(btrim(customeropno)) COLLATE "C",
                     customerno COLLATE "C",customeropno COLLATE "C";
            """, connection);
        Owner(command, customer, mapping);
        using var reader = command.ExecuteReader(); var rows = new List<OpB03HeaderDto>();
        while (reader.Read())
        {
            if (reader.GetInt64(11) > 1) throw Rule(409, "客戶站點複合識別碼存在大小寫／空白歧義，請先釐清來源資料。");
            rows.Add(MapHeader(reader));
        }
        return rows;
    }

    public OpB03HeaderDto Get(string customerNo, string customerOPNo)
    {
        using var connection = dataSource.OpenConnection();
        return ReadHeader(connection, null, Key(customerNo, "CustomerNo", 50), Key(customerOPNo, "CustomerOPNo", 20));
    }

    public OpB03DetailsDto Details(string customerNo, string customerOPNo)
    {
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var parent = ReadHeader(connection, transaction, Key(customerNo, "CustomerNo", 50), Key(customerOPNo, "CustomerOPNo", 20), share: true);
        AssertChildOwners(connection, transaction, parent);
        using var command = new NpgsqlCommand($"SELECT {DetailColumns} FROM {DetailTable} WHERE {ExactOwner} ORDER BY lower(btrim(opno)) COLLATE \"C\",opno COLLATE \"C\";", connection, transaction);
        Owner(command, parent.CustomerNo, parent.CustomerOPNo);
        var rows = new List<OpB03DetailDto>();
        using (var reader = command.ExecuteReader()) while (reader.Read()) rows.Add(MapDetail(reader));
        transaction.Commit(); return new(parent, rows);
    }

    public OpB03HeaderDto Create(OpB03HeaderWriteRequest request, string actor)
    {
        actor = Actor(actor);
        var customer = Key(request.CustomerNo, "CustomerNo", 50);
        var mapping = Key(request.CustomerOPNo, "CustomerOPNo", 20).ToUpperInvariant();
        var name = RequiredText(request.CustomerOPName, "Customer OP Name", 50);
        var ps = Key(request.PsNo, "PSNo", 50);
        var description = Description(request.Description); ValidateSequence(request.CustomerOPSeq);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Acquire(connection, transaction, customer, mapping);
        using (var check = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {HeaderTable} WHERE {OwnerMatch}) OR EXISTS(SELECT 1 FROM {DetailTable} WHERE {OwnerMatch});", connection, transaction))
        {
            Owner(check, customer, mapping);
            if (check.ExecuteScalar() is true) throw Rule(409, "客戶站點已存在或含無主檔明細，不能接管來源資料。");
        }
        customer = ApprovedCustomer(connection, transaction, customer);
        ps = ApprovedSegment(connection, transaction, ps);
        using var command = new NpgsqlCommand($"""
            INSERT INTO {HeaderTable}
              (customerno,customeropno,customeropname,customeropseq,psno,description,creator,createdate,datastamp,reviser,revisedate)
            VALUES(@customer,@mapping,@name,@sequence,@ps,@description,@actor,now() AT TIME ZONE 'UTC',0,NULL,NULL)
            RETURNING {HeaderColumns};
            """, connection, transaction);
        BindHeader(command, customer, mapping, name, request.CustomerOPSeq, ps, description, actor);
        var result = ReturnedHeader(command); transaction.Commit(); return result;
    }

    public OpB03HeaderDto Update(string customerNo, string customerOPNo, OpB03HeaderUpdateRequest request, string actor)
    {
        actor = Actor(actor); ValidateStamp(request.DataStamp);
        var name = RequiredText(request.CustomerOPName, "Customer OP Name", 50);
        var description = Description(request.Description); ValidateSequence(request.CustomerOPSeq);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var current = LockParent(connection, transaction, customerNo, customerOPNo);
        Expected(current.DataStamp, request.DataStamp); Incrementable(current.DataStamp);
        if (!SameKey(connection, transaction, current.CustomerNo, request.CustomerNo)
            || !SameKey(connection, transaction, current.CustomerOPNo, request.CustomerOPNo))
            throw Rule(400, "修改時不可變更 CustomerNo 或 CustomerOPNo。");
        // Do not require a still-active/missing legacy customer or unchanged PS.
        var ps = current.PsNo;
        if (!SameKey(connection, transaction, current.PsNo, request.PsNo))
        {
            using (var check = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {DetailTable} WHERE {OwnerMatch});", connection, transaction))
            {
                Owner(check, current.CustomerNo, current.CustomerOPNo);
                if (check.ExecuteScalar() is true) throw Rule(409, "此客戶站點已有明細，不可變更 PSNo。");
            }
            ps = ApprovedSegment(connection, transaction, Key(request.PsNo, "PSNo", 50));
        }
        using var command = new NpgsqlCommand($"""
            UPDATE {HeaderTable}
            SET customeropname=@name,customeropseq=@sequence,psno=@ps,description=@description,
                reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE {ExactOwner} AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {HeaderColumns};
            """, connection, transaction);
        BindHeader(command, current.CustomerNo, current.CustomerOPNo, name, request.CustomerOPSeq, ps, description, actor);
        Number(command, "stamp", request.DataStamp);
        var result = ReturnedHeader(command); transaction.Commit(); return result;
    }

    public void Delete(string customerNo, string customerOPNo, OpB03ExpectedRequest request, string actor)
    {
        _ = Actor(actor); ValidateStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var current = LockParent(connection, transaction, customerNo, customerOPNo);
        Expected(current.DataStamp, request.DataStamp);
        // No stamp increment: a matching maximum stamp may still be hard-deleted.
        using (var children = new NpgsqlCommand($"DELETE FROM {DetailTable} WHERE {ExactOwner};", connection, transaction))
        { Owner(children, current.CustomerNo, current.CustomerOPNo); children.ExecuteNonQuery(); }
        using (var command = new NpgsqlCommand($"DELETE FROM {HeaderTable} WHERE {ExactOwner} AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction))
        {
            Owner(command, current.CustomerNo, current.CustomerOPNo); Number(command, "stamp", request.DataStamp);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
        }
        transaction.Commit();
    }

    public OpB03DetailSavedDto AddDetail(string customerNo, string customerOPNo, OpB03DetailWriteRequest request, string actor)
    {
        actor = Actor(actor); ValidateStamp(request.ParentDataStamp);
        var op = Key(request.OpNo, "OPNo", 20);
        var code = RequiredText(request.CustOPProcessCode, "CustOPProcessCode", 20);
        var step = RequiredText(request.CustStepName, "CustStepName", 20);
        ValidateFlag(request.TransactionFlag, null);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var parent = LockParent(connection, transaction, customerNo, customerOPNo);
        Expected(parent.DataStamp, request.ParentDataStamp); Incrementable(parent.DataStamp);
        op = ApprovedOperation(connection, transaction, op, parent.PsNo);
        if (ReadDetail(connection, transaction, parent, op) is not null) throw Rule(409, "此客戶站點已包含該作業站。");
        using var command = new NpgsqlCommand($"""
            INSERT INTO {DetailTable}
              (customerno,customeropno,psno,opno,custopprocesscode,custstepname,transactionflag,creator,createdate,datastamp,reviser,revisedate)
            VALUES(@customer,@mapping,@ps,@op,@code,@step,@flag,@actor,now() AT TIME ZONE 'UTC',0,NULL,NULL)
            RETURNING {DetailColumns};
            """, connection, transaction);
        BindDetail(command, parent, parent.PsNo, op, code, step, request.TransactionFlag, actor);
        var item = ReturnedDetail(command);
        var savedParent = BumpParent(connection, transaction, parent, actor);
        transaction.Commit(); return new(savedParent, item);
    }

    public OpB03DetailSavedDto UpdateDetail(string customerNo, string customerOPNo, string opNo,
        OpB03DetailUpdateRequest request, string actor)
    {
        actor = Actor(actor); ValidateStamp(request.ParentDataStamp); ValidateStamp(request.DataStamp);
        var oldNo = Key(opNo, "舊 OPNo", 20);
        var code = RequiredText(request.CustOPProcessCode, "CustOPProcessCode", 20);
        var step = RequiredText(request.CustStepName, "CustStepName", 20);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var parent = LockParent(connection, transaction, customerNo, customerOPNo);
        Expected(parent.DataStamp, request.ParentDataStamp); Incrementable(parent.DataStamp);
        // Parent serializes all B3 writers. Inspect without a child row lock so
        // a changed lookup can be SHARE-locked before taking the child UPDATE.
        var observed = ReadDetail(connection, transaction, parent, oldNo) ?? throw MissingDetail();
        var op = observed.OpNo;
        if (!SameKey(connection, transaction, observed.OpNo, request.OpNo))
        {
            op = ApprovedOperation(connection, transaction, Key(request.OpNo, "OPNo", 20), parent.PsNo);
            if (ReadDetail(connection, transaction, parent, op) is not null) throw Rule(409, "變更後的作業站已存在於此客戶站點。");
        }
        var current = ReadDetail(connection, transaction, parent, oldNo, update: true) ?? throw MissingDetail();
        if (current.OpNo != observed.OpNo) throw Conflict();
        Expected(current.DataStamp, request.DataStamp); Incrementable(current.DataStamp);
        ValidateFlag(request.TransactionFlag, current.TransactionFlag);
        using var command = new NpgsqlCommand($"""
            UPDATE {DetailTable}
            SET opno=@op,custopprocesscode=@code,custstepname=@step,transactionflag=@flag,
                reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE {ExactOwner} AND opno=@oldOp AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {DetailColumns};
            """, connection, transaction);
        // Native Edit deliberately does NOT rewrite the existing child's PSNo.
        BindDetail(command, parent, current.PsNo, op, code, step, request.TransactionFlag, actor);
        Text(command, "oldOp", current.OpNo); Number(command, "stamp", request.DataStamp);
        var item = ReturnedDetail(command);
        var savedParent = BumpParent(connection, transaction, parent, actor);
        transaction.Commit(); return new(savedParent, item);
    }

    public OpB03DetailDeletedDto DeleteDetail(string customerNo, string customerOPNo, string opNo,
        OpB03DetailExpectedRequest request, string actor)
    {
        actor = Actor(actor); ValidateStamp(request.ParentDataStamp); ValidateStamp(request.DataStamp);
        var no = Key(opNo, "OPNo", 20);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var parent = LockParent(connection, transaction, customerNo, customerOPNo);
        Expected(parent.DataStamp, request.ParentDataStamp); Incrementable(parent.DataStamp);
        var current = ReadDetail(connection, transaction, parent, no, update: true) ?? throw MissingDetail();
        Expected(current.DataStamp, request.DataStamp);
        using (var command = new NpgsqlCommand($"DELETE FROM {DetailTable} WHERE {ExactOwner} AND opno=@op AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction))
        {
            Owner(command, parent.CustomerNo, parent.CustomerOPNo); Text(command, "op", current.OpNo); Number(command, "stamp", request.DataStamp);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
        }
        var savedParent = BumpParent(connection, transaction, parent, actor);
        transaction.Commit(); return new(savedParent);
    }

    public IReadOnlyList<OpB03CustomerOption> Customers()
    {
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("""
            SELECT customerno,customername,issuestate::int,count(*) OVER(PARTITION BY lower(btrim(customerno)))
            FROM "YMES".tblentcustomerbasis ORDER BY lower(btrim(customerno)) COLLATE "C",customerno COLLATE "C";
            """, connection);
        using var reader = command.ExecuteReader(); var rows = new List<OpB03CustomerOption>();
        while (reader.Read())
        {
            if (Int(reader, 2) != 2) continue;
            if (reader.GetInt64(3) > 1) throw Rule(409, "客戶主檔存在大小寫／空白歧義。");
            rows.Add(new(reader.GetString(0), String(reader, 1), 2));
        }
        return rows;
    }

    public IReadOnlyList<OpB03SegmentOption> Segments()
    {
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("""
            SELECT psno,psname,issuestate::int,count(*) OVER(PARTITION BY lower(btrim(psno)))
            FROM "YMES".tblprsproductionsegment ORDER BY lower(btrim(psno)) COLLATE "C",psno COLLATE "C";
            """, connection);
        using var reader = command.ExecuteReader(); var rows = new List<OpB03SegmentOption>();
        while (reader.Read())
        {
            if (Int(reader, 2) != 2) continue;
            if (reader.GetInt64(3) > 1) throw Rule(409, "生產區段存在大小寫／空白歧義。");
            rows.Add(new(reader.GetString(0), String(reader, 1), 2));
        }
        return rows;
    }

    public IReadOnlyList<OpB03OperationOption> Operations(string psNo)
    {
        // This is a stored relationship value: preserve its literal padding.
        if (psNo is null || psNo.Length > 50 || psNo.Any(char.IsControl)) throw Rule(400, "PSNo 必須提供且最多50字元。");
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("""
            WITH operations AS (
              SELECT opno,opname,psno,issuestate,
                count(*) OVER(PARTITION BY lower(btrim(opno))) AS aliases FROM "YMES".tblopbasis)
            SELECT opno,opname,psno,issuestate::int,aliases FROM operations
            WHERE issuestate=2 AND psno=@ps
            ORDER BY lower(btrim(opno)) COLLATE "C",opno COLLATE "C";
            """, connection);
        Text(command, "ps", psNo); using var reader = command.ExecuteReader(); var rows = new List<OpB03OperationOption>();
        while (reader.Read())
        {
            if (reader.GetInt64(4) > 1) throw Rule(409, "作業站主檔存在大小寫／空白歧義。");
            rows.Add(new(reader.GetString(0), String(reader, 1), String(reader, 2), Int(reader, 3)));
        }
        // Composite detail PK permits this OP under other customer mappings.
        return rows;
    }

    private static void Acquire(NpgsqlConnection connection, NpgsqlTransaction transaction, string customer, string mapping)
    {
        using var command = new NpgsqlCommand("""
            SELECT pg_advisory_xact_lock(hashtextextended(
              'YMES:OPB03:Parent:' || jsonb_build_array(lower(btrim(@customer)),lower(btrim(@mapping)))::text,0));
            """, connection, transaction);
        Owner(command, customer, mapping); command.ExecuteNonQuery();
    }

    private static OpB03HeaderDto LockParent(NpgsqlConnection connection, NpgsqlTransaction transaction, string customer, string mapping)
    {
        customer = Key(customer, "CustomerNo", 50); mapping = Key(mapping, "CustomerOPNo", 20);
        Acquire(connection, transaction, customer, mapping);
        var parent = ReadHeader(connection, transaction, customer, mapping, update: true);
        AssertChildOwners(connection, transaction, parent); return parent;
    }

    private static OpB03HeaderDto ReadHeader(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        string customer, string mapping, bool update = false, bool share = false)
    {
        using var command = new NpgsqlCommand($"SELECT {HeaderColumns} FROM {HeaderTable} WHERE {OwnerMatch} ORDER BY customerno COLLATE \"C\",customeropno COLLATE \"C\"" + (update ? " FOR UPDATE;" : share ? " FOR SHARE;" : ";"), connection, transaction);
        Owner(command, customer, mapping); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到指定客戶站點對應。");
        var row = MapHeader(reader);
        if (reader.Read()) throw Rule(409, "客戶站點複合識別碼存在大小寫／空白歧義，原資料已保留。");
        return row;
    }

    private static void AssertChildOwners(NpgsqlConnection connection, NpgsqlTransaction transaction, OpB03HeaderDto parent)
    {
        using var command = new NpgsqlCommand($"""
            SELECT EXISTS(SELECT 1 FROM {DetailTable}
              WHERE {OwnerMatch} AND (customerno<>@customer OR customeropno<>@mapping))
              OR EXISTS(SELECT 1 FROM {DetailTable} WHERE {ExactOwner}
                GROUP BY lower(btrim(opno)) HAVING count(*)>1);
            """, connection, transaction);
        Owner(command, parent.CustomerNo, parent.CustomerOPNo);
        if (command.ExecuteScalar() is true) throw Rule(409, "既有明細含客戶站點或作業站大小寫／空白別名，不能忽略、接管或重寫。");
    }

    private static OpB03DetailDto? ReadDetail(NpgsqlConnection connection, NpgsqlTransaction transaction,
        OpB03HeaderDto parent, string op, bool update = false)
    {
        using var command = new NpgsqlCommand($"SELECT {DetailColumns} FROM {DetailTable} WHERE {ExactOwner} AND lower(btrim(opno))=lower(btrim(@op)) ORDER BY opno COLLATE \"C\"" + (update ? " FOR UPDATE;" : ";"), connection, transaction);
        Owner(command, parent.CustomerNo, parent.CustomerOPNo); Text(command, "op", op);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var row = MapDetail(reader);
        if (reader.Read()) throw Rule(409, "此客戶站點的作業站明細含大小寫／空白歧義。");
        return row;
    }

    private static string ApprovedCustomer(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT customerno,issuestate::int FROM \"YMES\".tblentcustomerbasis WHERE lower(btrim(customerno))=lower(btrim(@no)) ORDER BY customerno COLLATE \"C\" FOR SHARE;", connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(422, "客戶不存在。");
        var actual = reader.GetString(0); var state = Int(reader, 1);
        if (reader.Read()) throw Rule(409, "客戶主檔存在大小寫／空白歧義。");
        if (state != 2) throw Rule(422, "新增客戶站點必須選擇已核准的真實客戶。");
        return actual;
    }

    private static string ApprovedSegment(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT psno,issuestate::int FROM \"YMES\".tblprsproductionsegment WHERE lower(btrim(psno))=lower(btrim(@no)) ORDER BY psno COLLATE \"C\" FOR SHARE;", connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(422, "生產區段不存在。");
        var actual = reader.GetString(0); var state = Int(reader, 1);
        if (reader.Read()) throw Rule(409, "生產區段存在大小寫／空白歧義。");
        if (state != 2) throw Rule(422, "新增或變更區段必須選擇已核准的真實生產區段。");
        return actual;
    }

    private static string ApprovedOperation(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, string ps)
    {
        using var command = new NpgsqlCommand("SELECT opno,psno,issuestate::int FROM \"YMES\".tblopbasis WHERE lower(btrim(opno))=lower(btrim(@no)) ORDER BY opno COLLATE \"C\" FOR SHARE;", connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(422, "作業站不存在。");
        var actual = reader.GetString(0); var actualPs = String(reader, 1); var state = Int(reader, 2);
        if (reader.Read()) throw Rule(409, "作業站主檔存在大小寫／空白歧義。");
        if (state != 2) throw Rule(422, "新增或變更明細必須選擇已核准的真實作業站。");
        if (!string.Equals(actualPs, ps, StringComparison.Ordinal)) throw Rule(422, "作業站 PSNo 必須與主檔字面相等；* 不是萬用值。");
        return actual;
    }

    private static OpB03HeaderDto BumpParent(NpgsqlConnection connection, NpgsqlTransaction transaction, OpB03HeaderDto parent, string actor)
    {
        using var command = new NpgsqlCommand($"""
            UPDATE {HeaderTable} SET reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE {ExactOwner} AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {HeaderColumns};
            """, connection, transaction);
        Owner(command, parent.CustomerNo, parent.CustomerOPNo); Text(command, "actor", actor); Number(command, "stamp", parent.DataStamp);
        return ReturnedHeader(command);
    }

    private static bool SameKey(NpgsqlConnection connection, NpgsqlTransaction transaction, string? left, string? right)
    {
        using var command = new NpgsqlCommand("SELECT lower(btrim(@left)) IS NOT DISTINCT FROM lower(btrim(@right));", connection, transaction);
        Text(command, "left", left); Text(command, "right", right); return command.ExecuteScalar() is true;
    }
    private static string Key(string? value, string label, int length)
    {
        var result = value?.Trim(' ') ?? "";
        if (result.Length is < 1 || result.Length > length || result.Any(char.IsControl)) throw Rule(400, $"{label} 必填、最多{length}字元且不可含控制字元。");
        return result;
    }
    private static string? Search(string? value, string label, int length)
    {
        var result = value?.Trim(' ');
        if (result?.Length > length || result?.Any(char.IsControl) == true) throw Rule(400, $"{label} 查詢最多{length}字元且不可含控制字元。");
        return string.IsNullOrEmpty(result) ? null : result;
    }
    private static string RequiredText(string? value, string label, int length)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > length || value.Any(char.IsControl)) throw Rule(400, $"{label} 必填、最多{length}字元且不可含控制字元。");
        return value;
    }
    private static string? Description(string? value)
    {
        if (value?.Length > 255 || value?.Any(c => char.IsControl(c) && c is not '\r' and not '\n' and not '\t') == true) throw Rule(400, "說明最多255字元且不可含無效控制字元。");
        return value; // NULL and empty string are not silently interchanged.
    }
    private static void ValidateSequence(int? value)
    { if (value is null or < 0 or > 999) throw Rule(400, "CustomerOPSeq 必須明確為0–999整數，符合來源 numeric(3,0)。"); }
    private static void ValidateFlag(int? value, int? previous)
    {
        if (value is null) throw Rule(400, "TransactionFlag 必須明確提供整數。");
        if (value is not (0 or 1) && value != previous) throw Rule(400, "新增或變更 TransactionFlag 只允許0或1；既有來源原值可原樣保留。");
    }
    private static void ValidateStamp(decimal? value)
    { if (value is { } stamp && (stamp != decimal.Truncate(stamp) || stamp < -MaxStamp || stamp > MaxStamp)) throw Rule(400, "DataStamp 必須明確為 NULL 或 numeric(11,0) 範圍內整數。"); }
    private static void Expected(decimal? actual, decimal? expected)
    { if (actual != expected) throw Conflict(); }
    private static void Incrementable(decimal? stamp)
    { ValidateStamp(stamp); if (stamp == MaxStamp) throw Rule(409, "DataStamp 已達來源上限，資料未變更。"); }
    private static string Actor(string actor) => Key(actor, "登入使用者", 10);
    private static string? String(NpgsqlDataReader reader, int i) => reader.IsDBNull(i) ? null : reader.GetString(i);
    private static int? Int(NpgsqlDataReader reader, int i) => reader.IsDBNull(i) ? null : reader.GetInt32(i);
    private static DateTime Date(NpgsqlDataReader reader, int i) => DateTime.SpecifyKind(reader.GetDateTime(i), DateTimeKind.Utc);
    private static DateTime? NullableDate(NpgsqlDataReader reader, int i) => reader.IsDBNull(i) ? null : Date(reader, i);
    private static decimal? Stamp(NpgsqlDataReader reader, int i) => reader.IsDBNull(i) ? null : reader.GetDecimal(i);
    private static OpB03HeaderDto MapHeader(NpgsqlDataReader r) => new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3), r.GetString(4), String(r, 5), r.GetString(6), Date(r, 7), Stamp(r, 8), String(r, 9), NullableDate(r, 10));
    private static OpB03DetailDto MapDetail(NpgsqlDataReader r) => new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5), r.GetInt32(6), r.GetString(7), Date(r, 8), Stamp(r, 9), String(r, 10), NullableDate(r, 11));
    private static OpB03HeaderDto ReturnedHeader(NpgsqlCommand command)
    { using var reader = command.ExecuteReader(); if (!reader.Read()) throw Conflict(); var row = MapHeader(reader); if (reader.Read()) throw Conflict(); return row; }
    private static OpB03DetailDto ReturnedDetail(NpgsqlCommand command)
    { using var reader = command.ExecuteReader(); if (!reader.Read()) throw Conflict(); var row = MapDetail(reader); if (reader.Read()) throw Conflict(); return row; }
    private static void Owner(NpgsqlCommand command, string? customer, string? mapping)
    { Text(command, "customer", customer); Text(command, "mapping", mapping); }
    private static void BindHeader(NpgsqlCommand command, string customer, string mapping, string name,
        int? sequence, string ps, string? description, string actor)
    {
        Owner(command, customer, mapping); Text(command, "name", name); Number(command, "sequence", sequence);
        Text(command, "ps", ps); Text(command, "description", description); Text(command, "actor", actor);
    }
    private static void BindDetail(NpgsqlCommand command, OpB03HeaderDto parent, string ps, string op,
        string code, string step, int? flag, string actor)
    {
        Owner(command, parent.CustomerNo, parent.CustomerOPNo); Text(command, "ps", ps); Text(command, "op", op);
        Text(command, "code", code); Text(command, "step", step); Number(command, "flag", flag); Text(command, "actor", actor);
    }
    private static void Text(NpgsqlCommand command, string name, string? value) => command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand command, string name, decimal? value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static OpB03RuleException Rule(int status, string message) => new(status, message);
    private static OpB03RuleException MissingDetail() => Rule(404, "找不到指定客戶站點作業站明細。");
    private static OpB03RuleException Conflict() => Rule(409, "主檔或明細已被其他操作更新，請重新查詢；本次未保存。");
}
