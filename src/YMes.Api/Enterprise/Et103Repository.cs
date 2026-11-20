using System.Data;
using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Enterprise;

public sealed class Et103Repository(NpgsqlDataSource dataSource)
{
    private const decimal MaxStamp = 99_999_999_999m;
    private const string Table = "\"YMES\".tblentbrokerbasis";
    private const string Columns = "brokerno,contactorname,telno,faxno,email,address,bankno,issuestate::int,creator,createdate,datastamp,reviser,revisedate";
    private static readonly string[] RelationNames = ["tblentbrokerbasis", "tblentcustomerbasis", "tblentforwarderbasis"];
    private static readonly string[] ReferenceNames = ["brokerno", "brokerassigned", "broker"];

    public IReadOnlyList<Et103BrokerDto> List(string? brokerNo, string? issueState)
    {
        var search = Search(brokerNo); var state = FilterState(issueState);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        EnsureRegistry(connection, transaction);
        using var command = new NpgsqlCommand($"""
            WITH rows AS (
              SELECT *,count(*) OVER(PARTITION BY lower(btrim(brokerno))) AS aliases FROM {Table})
            SELECT {Columns},aliases FROM rows
            WHERE (@state IS NULL OR issuestate=@state)
              AND (@search IS NULL OR position(lower(@search) in lower(brokerno))>0)
            ORDER BY lower(btrim(brokerno)) COLLATE "C",brokerno COLLATE "C";
            """, connection, transaction);
        Text(command, "search", search); Number(command, "state", state);
        var items = new List<Et103BrokerDto>();
        using (var reader = command.ExecuteReader()) while (reader.Read())
        {
            if (reader.GetInt64(13) > 1) throw Alias();
            items.Add(Map(reader));
        }
        transaction.Commit(); return items;
    }

    public Et103BrokerDto Get(string brokerNo)
    {
        var no = Key(brokerNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        EnsureRegistry(connection, transaction);
        var row = Read(connection, transaction, no);
        transaction.Commit(); return row;
    }

    public Et103BrokerDto Create(Et103WriteRequest request, string actor)
    {
        var raw = Key(request.BrokerNo); actor = Actor(actor); ValidateFields(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        // PostgreSQL is the authority for new-key case conversion. Validate the
        // result again because a Unicode uppercase can expand the source key.
        var no = NewKey(connection, transaction, raw);
        Acquire(connection, transaction, no);
        EnsureRegistry(connection, transaction);
        using (var duplicate = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {Table} WHERE lower(btrim(brokerno))=lower(btrim(@no)));", connection, transaction))
        { Text(duplicate, "no", no); if (duplicate.ExecuteScalar() is true) throw Rule(409, "報關行識別碼已存在或有大小寫／空白別名，不能覆寫來源列。"); }
        using var command = new NpgsqlCommand($"""
            INSERT INTO {Table}(brokerno,contactorname,telno,faxno,email,address,bankno,issuestate,creator,createdate,datastamp,reviser,revisedate)
            VALUES(@no,@contactor,@tel,@fax,@email,@address,@bank,0,@actor,now() AT TIME ZONE 'UTC',0,NULL,NULL)
            RETURNING {Columns};
            """, connection, transaction);
        Bind(command, no, request, actor);
        var result = Returned(command); transaction.Commit(); return result;
    }

    public Et103BrokerDto Update(string brokerNo, Et103UpdateRequest request, string actor)
    {
        var no = Key(brokerNo); actor = Actor(actor); ValidateStamp(request.DataStamp); ValidateFields(request);
        if (string.IsNullOrWhiteSpace(request.BrokerNo) || request.BrokerNo.Length>50 || request.BrokerNo.Any(char.IsControl))
            throw Rule(400, "BrokerNo 必填、最多50字元且不可含控制字元。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Acquire(connection, transaction, no); EnsureRegistry(connection, transaction);
        var current = Read(connection, transaction, no, update: true);
        KnownState(current.IssueState); ExpectedStamp(current.DataStamp, request.DataStamp);
        RequireDraft(current.IssueState); Incrementable(current.DataStamp);
        if (!string.Equals(request.BrokerNo, current.BrokerNo, StringComparison.Ordinal))
            throw Rule(422, "修改時不可變更 BrokerNo，請保留原始識別碼（含大小寫與空白）。");
        using var command = new NpgsqlCommand($"""
            UPDATE {Table}
            SET contactorname=@contactor,telno=@tel,faxno=@fax,email=@email,address=@address,bankno=@bank,
                reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE brokerno=@no AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {Columns};
            """, connection, transaction);
        // Never normalize/re-key an existing raw identifier or rewrite its
        // creation audit/unknown extension columns during a metadata edit.
        Bind(command, current.BrokerNo, request, actor);
        Number(command, "state", current.IssueState); Number(command, "stamp", request.DataStamp);
        var result = Returned(command); transaction.Commit(); return result;
    }

    public Et103BrokerDto Approve(string brokerNo, Et103ExpectedRequest request, string actor)
    {
        var no = Key(brokerNo); actor = Actor(actor); ValidateStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Acquire(connection, transaction, no); EnsureRegistry(connection, transaction);
        var current = Read(connection, transaction, no, update: true);
        KnownState(current.IssueState); ExpectedState(current.IssueState, request.ExpectedIssueState);
        ExpectedStamp(current.DataStamp, request.DataStamp); RequireDraft(current.IssueState); Incrementable(current.DataStamp);
        // Native no-Job direct approval is usable only when no relevant actual
        // catalog candidate exists. A named/pattern candidate is not a guessed
        // empty Job or a successful notification; provider delivery is required.
        if (WorkflowMayExist(connection, transaction))
            throw Rule(503, "已偵測到簽核來源，但尚未提供此功能的簽核服務；本次未核准或建立簽核單。");
        var result = ChangeState(connection, transaction, current, 2, actor);
        transaction.Commit(); return result;
    }

    public Et103DeleteResult Delete(string brokerNo, Et103ExpectedRequest request, string actor)
    {
        var no = Key(brokerNo); actor = Actor(actor); ValidateStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Acquire(connection, transaction, no); var shipAddressPresent = EnsureRegistry(connection, transaction);
        var current = Read(connection, transaction, no, update: true);
        KnownState(current.IssueState); ExpectedState(current.IssueState, request.ExpectedIssueState);
        ExpectedStamp(current.DataStamp, request.DataStamp);
        if (current.IssueState == 1) throw Rule(409, "簽核中的報關行不可刪除。");
        if (current.IssueState == 2)
        {
            // Existing references do not prevent native2→-1 retirement.
            Incrementable(current.DataStamp);
            var saved = ChangeState(connection, transaction, current, -1, actor);
            transaction.Commit(); return new(false, saved);
        }
        // Explicit YMES integrity protection, not a claimed native delete rule.
        // Future reference writers must acquire this same Broker namespace and
        // parent SHARE before checking state2 and inserting through commit.
        if (HasReferences(connection, transaction, current.BrokerNo, shipAddressPresent))
            throw Rule(409, "客戶、承攬商或出貨地址仍引用此報關行，資料已保留，不能實體刪除。");
        using (var command = new NpgsqlCommand($"DELETE FROM {Table} WHERE brokerno=@no AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction))
        {
            Text(command, "no", current.BrokerNo); Number(command, "state", request.ExpectedIssueState); Number(command, "stamp", request.DataStamp);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
        }
        // Hard delete performs no bump, so a matching positive maximum is valid.
        transaction.Commit(); return new(true, null);
    }

    private static Et103BrokerDto ChangeState(NpgsqlConnection connection, NpgsqlTransaction transaction, Et103BrokerDto current, int next, string actor)
    {
        using var command = new NpgsqlCommand($"""
            UPDATE {Table} SET issuestate=@next,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE brokerno=@no AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {Columns};
            """, connection, transaction);
        Text(command, "no", current.BrokerNo); Text(command, "actor", actor);
        Number(command, "next", next); Number(command, "state", current.IssueState); Number(command, "stamp", current.DataStamp);
        return Returned(command);
    }

    private static Et103BrokerDto Read(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, bool update = false)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM {Table} WHERE lower(btrim(brokerno))=lower(btrim(@no)) ORDER BY brokerno COLLATE \"C\"" + (update ? " FOR UPDATE;" : ";"), connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到指定報關行。");
        var row = Map(reader); if (reader.Read()) throw Alias(); return row;
    }

    private static string NewKey(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT upper(btrim(@new));", connection, transaction);
        Text(command, "new", no);
        var normalized = command.ExecuteScalar() as string;
        if (normalized is null || string.IsNullOrWhiteSpace(normalized) || normalized.Length>50 || normalized.Any(char.IsControl))
            throw Rule(400, "BrokerNo 經資料庫大寫處理後必須為1–50字元且不可含控制字元。");
        return normalized;
    }

    private static void Acquire(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:ENT:Broker:' || lower(btrim(@no)),0));", connection, transaction);
        Text(command, "no", no); command.ExecuteNonQuery();
    }

    private static bool HasReferences(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, bool shipAddressPresent)
    {
        using var command = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM "YMES".tblentcustomerbasis WHERE lower(btrim(brokerassigned))=lower(btrim(@no)))
                OR EXISTS(SELECT 1 FROM "YMES".tblentforwarderbasis WHERE lower(btrim(brokerno))=lower(btrim(@no)));
            """, connection, transaction);
        Text(command, "no", no);
        return command.ExecuteScalar() is true
            || ShipAddressReferenceGuard.HasReferences(connection, transaction, ShipAddressReference.Broker, no, shipAddressPresent);
    }

    private static bool WorkflowMayExist(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        using var command = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname NOT IN ('pg_catalog','information_schema')
                AND c.relkind IN ('r','p','v','m','f')
                AND (lower(c.relname)=ANY(@known) OR lower(c.relname) LIKE 'tblisu%'
                  OR lower(c.relname) LIKE '%issue%job%' OR lower(c.relname) LIKE '%job%issue%'));
            """, connection, transaction);
        command.Parameters.Add("known", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" };
        return command.ExecuteScalar() is true;
    }

    private static bool EnsureRegistry(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        // Catalog-only discovery: never read business data from an unreviewed
        // table/column or accept a same-fold object in another schema/kind/case.
        using (var command = new NpgsqlCommand("""
            SELECT n.nspname,c.relname,c.relkind::text,c.relrowsecurity,c.relforcerowsecurity
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND lower(c.relname)=ANY(@tables)
            ORDER BY n.nspname COLLATE "C",c.relname COLLATE "C";
            """, connection, transaction))
        {
            command.Parameters.Add("tables", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = RelationNames;
            var found = new HashSet<string>(StringComparer.Ordinal);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetString(0)!="YMES" || !RelationNames.Contains(reader.GetString(1),StringComparer.Ordinal)
                    || reader.GetString(2)!="r" || reader.GetBoolean(3) || reader.GetBoolean(4)
                    || !found.Add(reader.GetString(1))) throw RegistryUnavailable();
            }
            if (found.Count!=3) throw RegistryUnavailable();
        }
        var shipAddressPresent = ShipAddressReferenceGuard.EnsureOptional(connection, transaction, ShipAddressReference.Broker, RegistryUnavailable);
        var expectedReferences = new HashSet<(string Table,string Column,bool NotNull)>
        {
            ("tblentbrokerbasis","brokerno",true),("tblentcustomerbasis","brokerassigned",false),
            ("tblentforwarderbasis","brokerno",true)
        };
        if (shipAddressPresent) expectedReferences.Add(("tblentcustomershipaddr","broker",false));
        using (var command = new NpgsqlCommand("""
            SELECT n.nspname,c.relname,c.relkind::text,c.relrowsecurity,c.relforcerowsecurity,
                   a.attname,format_type(a.atttypid,a.atttypmod),a.attnotnull
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
            WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND c.relkind IN ('r','p','v','m','f')
              AND lower(a.attname)=ANY(@columns)
            ORDER BY n.nspname COLLATE "C",c.relname COLLATE "C",a.attnum;
            """, connection, transaction))
        {
            command.Parameters.Add("columns", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = ReferenceNames;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetString(0)!="YMES" || reader.GetString(2)!="r" || reader.GetBoolean(3) || reader.GetBoolean(4)
                    || reader.GetString(6)!="character varying(50)"
                    || !expectedReferences.Remove((reader.GetString(1),reader.GetString(5),reader.GetBoolean(7))))
                    throw RegistryUnavailable();
            }
            if (expectedReferences.Count!=0) throw RegistryUnavailable();
        }
        var expected = new Dictionary<string,(int Ordinal,string Type,bool NotNull)>(StringComparer.Ordinal)
        {
            ["brokerno"]=(1,"character varying(50)",true),["contactorname"]=(2,"character varying(50)",false),
            ["telno"]=(3,"character varying(25)",false),["faxno"]=(4,"character varying(25)",false),
            ["email"]=(5,"character varying(255)",false),["address"]=(6,"character varying(500)",false),
            ["bankno"]=(7,"character varying(20)",false),["issuestate"]=(8,"numeric(1,0)",false),
            ["creator"]=(9,"character varying(10)",false),["createdate"]=(10,"timestamp without time zone",false),
            ["datastamp"]=(11,"numeric(11,0)",false),["reviser"]=(12,"character varying(10)",false),
            ["revisedate"]=(13,"timestamp without time zone",false)
        };
        using (var command = new NpgsqlCommand("""
            SELECT a.attname,a.attnum::int,format_type(a.atttypid,a.atttypmod),a.attnotnull,a.attidentity::text,a.attgenerated::text
            FROM pg_catalog.pg_attribute a WHERE a.attrelid='"YMES".tblentbrokerbasis'::regclass
              AND a.attnum>0 AND NOT a.attisdropped ORDER BY a.attnum;
            """, connection, transaction))
        {
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (!expected.TryGetValue(reader.GetString(0),out var shape)) continue; // Preserve unrelated live extensions.
                if (shape.Ordinal!=reader.GetInt32(1) || shape.Type!=reader.GetString(2) || shape.NotNull!=reader.GetBoolean(3)
                    || reader.GetString(4)!="" || reader.GetString(5)!="") throw RegistryUnavailable();
                expected.Remove(reader.GetString(0));
            }
            if (expected.Count!=0) throw RegistryUnavailable();
        }
        // These zero-row SELECTs verify actual necessary read grants and retain
        // relation ACCESS SHARE locks to commit without requiring Forwarder
        // table SELECT or reading any unreviewed reference value.
        using var probe = new NpgsqlCommand($"SELECT {Columns} FROM {Table} WHERE false; SELECT brokerassigned FROM \"YMES\".tblentcustomerbasis WHERE false; SELECT brokerno FROM \"YMES\".tblentforwarderbasis WHERE false;", connection, transaction);
        probe.ExecuteNonQuery();
        return shipAddressPresent;
    }

    private static void ValidateFields(Et103WriteRequest request)
    {
        Optional(request.ContactorName,"ContactorName",50); Optional(request.TelNo,"TelNo",25);
        Optional(request.FaxNo,"FaxNo",25); Optional(request.Email,"Email",255);
        Optional(request.Address,"Address",500,multiline:true); Optional(request.BankNo,"BankNo",20);
    }
    private static void Optional(string? value,string label,int maximum,bool multiline=false)
    {
        if (value?.Length>maximum || value?.Any(c=>char.IsControl(c) && !(multiline && (c is '\r' or '\n' or '\t')))==true)
            throw Rule(422,$"{label} 最多{maximum}字元且不可含無效控制字元。");
    }
    private static string Key(string? value)
    {
        var key=value?.Trim(' ') ?? "";
        if (string.IsNullOrWhiteSpace(key) || key.Length>50 || key.Any(char.IsControl)) throw Rule(400,"BrokerNo 必填、最多50字元且不可含控制字元。");
        return key;
    }
    private static string? Search(string? value)
    {
        if (value is null) return null;
        if (value.Length>50 || value.Any(char.IsControl)) throw Rule(400,"BrokerNo 查詢最多50字元且不可含控制字元。");
        return value.Length==0 ? null : value; // Literal contains; no SQL wildcard or silent trim.
    }
    private static int? FilterState(string? value)
    {
        if (value is null) return null;
        if (!int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out var state) || state is not (-1 or 0 or 1 or 2))
            throw Rule(400,"IssueState 篩選只允許-1、0、1、2；省略時回傳所有原始狀態。");
        return state;
    }
    private static string Actor(string value)
    { if (string.IsNullOrWhiteSpace(value) || value.Length>10 || value.Any(char.IsControl)) throw Rule(400,"登入使用者識別碼不符合來源長度。"); return value; }
    private static void ValidateStamp(decimal? value)
    { if (value is { } stamp && (stamp!=decimal.Truncate(stamp) || stamp < -MaxStamp || stamp>MaxStamp)) throw Rule(400,"DataStamp 必須明確為NULL或來源numeric(11,0)範圍內整數。"); }
    private static void Incrementable(decimal? value)
    { ValidateStamp(value); if (value==MaxStamp) throw Rule(409,"DataStamp 已達來源上限，資料未變更。"); }
    private static void KnownState(int? value)
    { if (value is not (-1 or 0 or 1 or 2)) throw Rule(422,"來源IssueState為NULL或未知狀態，資料已保留。"); }
    private static void RequireDraft(int? value)
    { if (value!=0) throw Rule(409,"只有未簽核狀態0的報關行可修改或核准。"); }
    private static void ExpectedState(int? actual,int? expected)
    { if (actual!=expected) throw Conflict(); }
    private static void ExpectedStamp(decimal? actual,decimal? expected)
    { if (actual!=expected) throw Conflict(); }
    private static string? String(NpgsqlDataReader r,int i)=>r.IsDBNull(i)?null:r.GetString(i);
    private static DateTime? Date(NpgsqlDataReader r,int i)=>r.IsDBNull(i)?null:DateTime.SpecifyKind(r.GetDateTime(i),DateTimeKind.Utc);
    private static Et103BrokerDto Map(NpgsqlDataReader r)=>new(r.GetString(0),String(r,1),String(r,2),String(r,3),String(r,4),String(r,5),String(r,6),
        r.IsDBNull(7)?null:r.GetInt32(7),String(r,8),Date(r,9),r.IsDBNull(10)?null:r.GetDecimal(10),String(r,11),Date(r,12));
    private static Et103BrokerDto Returned(NpgsqlCommand command)
    { using var r=command.ExecuteReader(); if (!r.Read()) throw Conflict(); var row=Map(r); if (r.Read()) throw Conflict(); return row; }
    private static void Bind(NpgsqlCommand c,string no,Et103WriteRequest request,string actor)
    {
        Text(c,"no",no); Text(c,"contactor",request.ContactorName); Text(c,"tel",request.TelNo); Text(c,"fax",request.FaxNo);
        Text(c,"email",request.Email); Text(c,"address",request.Address); Text(c,"bank",request.BankNo); Text(c,"actor",actor);
    }
    private static void Text(NpgsqlCommand c,string name,string? value)=>c.Parameters.Add(name,NpgsqlDbType.Varchar).Value=(object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand c,string name,decimal? value)=>c.Parameters.Add(name,NpgsqlDbType.Numeric).Value=(object?)value ?? DBNull.Value;
    private static Et103RuleException Rule(int status,string detail)=>new(status,detail);
    private static Et103RuleException RegistryUnavailable()=>Rule(503,"報關行相依來源或引用對應尚未完成核對；本次未變更資料。");
    private static Et103RuleException Alias()=>Rule(409,"BrokerNo 存在大小寫／空白識別碼歧義，原始資料已保留。");
    private static Et103RuleException Conflict()=>Rule(409,"報關行狀態或版本已變更，請重新查詢；本次未保存。");
}
