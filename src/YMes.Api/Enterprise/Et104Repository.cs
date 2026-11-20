using System.Data;
using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Enterprise;

public sealed class Et104Repository(NpgsqlDataSource dataSource)
{
    private const decimal MaxStamp = 99_999_999_999m;
    private const string Table = "\"YMES\".tblentforwarderbasis";
    private const string Columns = "forwarderno,contactorname,telno,faxno,email,address,brokerno,bankno,issuestate::int,creator,createdate,acno,datastamp,reviser,revisedate";
    private static readonly string[] RelationNames = ["tblentforwarderbasis", "tblentcustomerbasis"];
    private static readonly string[] ReferenceNames = ["forwarderno", "forwarderassigned", "forwarder"];

    public IReadOnlyList<Et104ForwarderDto> List(string? forwarderNo, string? issueState)
    {
        var search = Search(forwarderNo); var state = FilterState(issueState);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        EnsureRegistry(connection, transaction);
        using var command = new NpgsqlCommand($"""
            WITH rows AS (
              SELECT *,count(*) OVER(PARTITION BY lower(btrim(forwarderno))) AS aliases FROM {Table})
            SELECT {Columns},aliases FROM rows
            WHERE (@state IS NULL OR issuestate=@state)
              AND (@search IS NULL OR position(lower(@search) in lower(forwarderno))>0)
            ORDER BY lower(btrim(forwarderno)) COLLATE "C",forwarderno COLLATE "C";
            """, connection, transaction);
        Text(command, "search", search); Number(command, "state", state);
        var items = new List<Et104ForwarderDto>();
        using (var reader = command.ExecuteReader()) while (reader.Read())
        {
            if (reader.GetInt64(15) > 1) throw Alias();
            items.Add(Map(reader));
        }
        transaction.Commit(); return items;
    }

    public Et104ForwarderDto Get(string forwarderNo)
    {
        var no = Key(forwarderNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        EnsureRegistry(connection, transaction);
        var row = Read(connection, transaction, no);
        transaction.Commit(); return row;
    }

    public Et104ForwarderDto Create(Et104WriteRequest request, string actor)
    {
        var raw = Key(request.ForwarderNo); var broker = Key(request.BrokerNo, "BrokerNo"); actor = Actor(actor); ValidateFields(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        // PostgreSQL is the authority for new-key case conversion. Validate the
        // result again because a Unicode uppercase can expand the source key.
        var no = NewKey(connection, transaction, raw);
        AcquireBroker(connection, transaction, broker);
        var parentBroker = ApprovedBroker(connection, transaction, broker);
        Acquire(connection, transaction, no);
        EnsureRegistry(connection, transaction);
        using (var duplicate = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {Table} WHERE lower(btrim(forwarderno))=lower(btrim(@no)));", connection, transaction))
        { Text(duplicate, "no", no); if (duplicate.ExecuteScalar() is true) throw Rule(409, "貨運行識別碼已存在或有大小寫／空白別名，不能覆寫來源列。"); }
        using var command = new NpgsqlCommand($"""
            INSERT INTO {Table}(forwarderno,contactorname,telno,faxno,email,address,brokerno,bankno,issuestate,creator,createdate,acno,datastamp,reviser,revisedate)
            VALUES(@no,@contactor,@tel,@fax,@email,@address,@broker,@bank,0,@actor,now() AT TIME ZONE 'UTC',@ac,0,NULL,NULL)
            RETURNING {Columns};
            """, connection, transaction);
        Bind(command, no, request, actor); Text(command, "broker", parentBroker);
        var result = Returned(command); transaction.Commit(); return result;
    }

    public Et104ForwarderDto Update(string forwarderNo, Et104UpdateRequest request, string actor)
    {
        var no = Key(forwarderNo); actor = Actor(actor); ValidateStamp(request.DataStamp); ValidateFields(request);
        // Existing writes create no Broker reference: own namespace only, no
        // parent existence/state/alias requalification or Broker row lock.
        if (string.IsNullOrWhiteSpace(request.ForwarderNo) || request.ForwarderNo.Length>50 || request.ForwarderNo.Any(char.IsControl))
            throw Rule(400, "ForwarderNo 必填、最多50字元且不可含控制字元。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Acquire(connection, transaction, no); EnsureRegistry(connection, transaction);
        var current = Read(connection, transaction, no, update: true);
        KnownState(current.IssueState); ExpectedStamp(current.DataStamp, request.DataStamp);
        RequireEditable(current.IssueState); Incrementable(current.DataStamp);
        if (!string.Equals(request.ForwarderNo, current.ForwarderNo, StringComparison.Ordinal)
            || !string.Equals(request.BrokerNo, current.BrokerNo, StringComparison.Ordinal))
            throw Rule(422, "修改時不可變更 ForwarderNo 或 BrokerNo，請保留兩個來源原始識別碼（含大小寫與空白）。");
        using var command = new NpgsqlCommand($"""
            UPDATE {Table}
            SET contactorname=@contactor,telno=@tel,faxno=@fax,email=@email,address=@address,bankno=@bank,acno=@ac,
                reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE forwarderno=@no AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {Columns};
            """, connection, transaction);
        // Never normalize/re-key an existing raw identifier or rewrite its
        // creation audit/unknown extension columns during a metadata edit.
        Bind(command, current.ForwarderNo, request, actor);
        Number(command, "state", current.IssueState); Number(command, "stamp", request.DataStamp);
        var result = Returned(command); transaction.Commit(); return result;
    }

    public Et104ForwarderDto Approve(string forwarderNo, Et104ExpectedRequest request, string actor)
    {
        var no = Key(forwarderNo); actor = Actor(actor); ValidateStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Acquire(connection, transaction, no); EnsureRegistry(connection, transaction);
        var current = Read(connection, transaction, no, update: true);
        KnownState(current.IssueState); ExpectedState(current.IssueState, request.ExpectedIssueState);
        ExpectedStamp(current.DataStamp, request.DataStamp); RequireDraft(current.IssueState); Incrementable(current.DataStamp);
        // IssueNo FORWARDER BASIS. Native no-Job direct approval is usable only when no relevant actual
        // catalog candidate exists. A named/pattern candidate is not a guessed
        // empty Job or a successful notification; provider delivery is required.
        if (WorkflowMayExist(connection, transaction))
            throw Rule(503, "已偵測到簽核來源，但尚未提供此功能的簽核服務；本次未核准或建立簽核單。");
        var result = ChangeState(connection, transaction, current, 2, actor);
        transaction.Commit(); return result;
    }

    public Et104DeleteResult Delete(string forwarderNo, Et104ExpectedRequest request, string actor)
    {
        var no = Key(forwarderNo); actor = Actor(actor); ValidateStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Acquire(connection, transaction, no); EnsureRegistry(connection, transaction);
        var current = Read(connection, transaction, no, update: true);
        KnownState(current.IssueState); ExpectedState(current.IssueState, request.ExpectedIssueState);
        ExpectedStamp(current.DataStamp, request.DataStamp);
        if (current.IssueState == 1) throw Rule(409, "簽核中的貨運行不可刪除。");
        if (current.IssueState == 2)
        {
            // Existing references do not prevent native2→-1 retirement.
            Incrementable(current.DataStamp);
            var saved = ChangeState(connection, transaction, current, -1, actor);
            transaction.Commit(); return new(false, saved);
        }
        // Explicit YMES integrity protection, not a claimed native delete rule.
        // Future Customer reference writers must acquire this Forwarder namespace and
        // parent SHARE before checking state2 and inserting through commit.
        if (HasReferences(connection, transaction, current.ForwarderNo))
            throw Rule(409, "客戶仍引用此貨運行，資料已保留，不能實體刪除。");
        using (var command = new NpgsqlCommand($"DELETE FROM {Table} WHERE forwarderno=@no AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction))
        {
            Text(command, "no", current.ForwarderNo); Number(command, "state", request.ExpectedIssueState); Number(command, "stamp", request.DataStamp);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
        }
        // Hard delete performs no bump, so a matching positive maximum is valid.
        transaction.Commit(); return new(true, null);
    }

    private static Et104ForwarderDto ChangeState(NpgsqlConnection connection, NpgsqlTransaction transaction, Et104ForwarderDto current, int next, string actor)
    {
        using var command = new NpgsqlCommand($"""
            UPDATE {Table} SET issuestate=@next,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE forwarderno=@no AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {Columns};
            """, connection, transaction);
        Text(command, "no", current.ForwarderNo); Text(command, "actor", actor);
        Number(command, "next", next); Number(command, "state", current.IssueState); Number(command, "stamp", current.DataStamp);
        return Returned(command);
    }

    private static Et104ForwarderDto Read(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, bool update = false)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM {Table} WHERE lower(btrim(forwarderno))=lower(btrim(@no)) ORDER BY forwarderno COLLATE \"C\"" + (update ? " FOR UPDATE;" : ";"), connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到指定貨運行。");
        var row = Map(reader); if (reader.Read()) throw Alias(); return row;
    }

    private static string NewKey(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT upper(btrim(@new));", connection, transaction);
        Text(command, "new", no);
        var normalized = command.ExecuteScalar() as string;
        if (normalized is null || string.IsNullOrWhiteSpace(normalized) || normalized.Length>50 || normalized.Any(char.IsControl))
            throw Rule(400, "ForwarderNo 經資料庫大寫處理後必須為1–50字元且不可含控制字元。");
        return normalized;
    }

    private static void Acquire(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:ENT:Forwarder:' || lower(btrim(@no)),0));", connection, transaction);
        Text(command, "no", no); command.ExecuteNonQuery();
    }

    private static void AcquireBroker(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:ENT:Broker:' || lower(btrim(@no)),0));", connection, transaction);
        Text(command,"no",no); command.ExecuteNonQuery();
    }

    private static string ApprovedBroker(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        // This helper is Create-only. Never call it from an immutable-reference
        // Edit/Delete/Approve merely to reuse validation code.
        using (var catalog = new NpgsqlCommand("""
            SELECT n.nspname,c.relname,c.relkind::text,c.relrowsecurity,c.relforcerowsecurity
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND lower(c.relname)='tblentbrokerbasis';
            """,connection,transaction))
        {
            using var reader=catalog.ExecuteReader();
            if (!reader.Read() || reader.GetString(0)!="YMES" || reader.GetString(1)!="tblentbrokerbasis"
                || reader.GetString(2)!="r" || reader.GetBoolean(3) || reader.GetBoolean(4) || reader.Read()) throw RegistryUnavailable();
        }
        using (var shape = new NpgsqlCommand("""
            SELECT a.attname,format_type(a.atttypid,a.atttypmod),a.attnotnull,a.attidentity::text,a.attgenerated::text
            FROM pg_catalog.pg_attribute a WHERE a.attrelid='"YMES".tblentbrokerbasis'::regclass
              AND a.attnum>0 AND NOT a.attisdropped AND lower(a.attname) IN ('brokerno','issuestate');
            """,connection,transaction))
        {
            var expected=new Dictionary<string,(string Type,bool NotNull)>(StringComparer.Ordinal)
            { ["brokerno"]=("character varying(50)",true),["issuestate"]=("numeric(1,0)",false) };
            using var reader=shape.ExecuteReader();
            while (reader.Read())
            {
                if (!expected.Remove(reader.GetString(0),out var e) || reader.GetString(1)!=e.Type
                    || reader.GetBoolean(2)!=e.NotNull || reader.GetString(3)!="" || reader.GetString(4)!="") throw RegistryUnavailable();
            }
            if (expected.Count!=0) throw RegistryUnavailable();
        }
        using var command=new NpgsqlCommand("""
            SELECT brokerno,issuestate::int FROM "YMES".tblentbrokerbasis
            WHERE lower(btrim(brokerno))=lower(btrim(@no)) ORDER BY brokerno COLLATE "C" FOR SHARE;
            """,connection,transaction);
        Text(command,"no",no); using var r=command.ExecuteReader();
        if (!r.Read()) throw Rule(422,"新增貨運行必須選擇實際存在的已核准報關行。");
        var raw=r.GetString(0); int? state=r.IsDBNull(1)?null:r.GetInt32(1);
        if (r.Read()) throw Rule(409,"BrokerNo 存在大小寫／空白歧義，不能任選新增引用來源。");
        if (state!=2) throw Rule(422,"新增貨運行必須選擇目前已核准狀態2的報關行。");
        return raw;
    }

    private static bool HasReferences(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("""
            SELECT EXISTS(SELECT 1 FROM "YMES".tblentcustomerbasis WHERE lower(btrim(forwarderassigned))=lower(btrim(@no)));
            """, connection, transaction);
        Text(command, "no", no); return command.ExecuteScalar() is true;
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

    private static void EnsureRegistry(NpgsqlConnection connection, NpgsqlTransaction transaction)
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
            if (found.Count!=2) throw RegistryUnavailable();
        }
        var expectedReferences = new HashSet<(string Table,string Column,bool NotNull)>
        {
            ("tblentforwarderbasis","forwarderno",true),("tblentcustomerbasis","forwarderassigned",false)
        };
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
            ["forwarderno"]=(1,"character varying(50)",true),["contactorname"]=(2,"character varying(50)",false),
            ["telno"]=(3,"character varying(25)",false),["faxno"]=(4,"character varying(25)",false),
            ["email"]=(5,"character varying(255)",false),["address"]=(6,"character varying(500)",false),
            ["brokerno"]=(7,"character varying(50)",true),["bankno"]=(8,"character varying(20)",false),
            ["issuestate"]=(9,"numeric(1,0)",false),["creator"]=(10,"character varying(10)",false),
            ["createdate"]=(11,"timestamp without time zone",false),["acno"]=(12,"character varying(50)",false),
            ["datastamp"]=(13,"numeric(11,0)",false),["reviser"]=(14,"character varying(10)",false),
            ["revisedate"]=(15,"timestamp without time zone",false)
        };
        using (var command = new NpgsqlCommand("""
            SELECT a.attname,a.attnum::int,format_type(a.atttypid,a.atttypmod),a.attnotnull,a.attidentity::text,a.attgenerated::text
            FROM pg_catalog.pg_attribute a WHERE a.attrelid='"YMES".tblentforwarderbasis'::regclass
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
        // Reviewed own/contact references only; existing operations never need
        // parent data/permissions or a parent row lock. Zero-row probes retain
        // ACCESS SHARE on the required relations to commit.
        using var probe = new NpgsqlCommand($"SELECT {Columns} FROM {Table} WHERE false; SELECT forwarderassigned FROM \"YMES\".tblentcustomerbasis WHERE false;", connection, transaction);
        probe.ExecuteNonQuery();
    }

    private static void ValidateFields(Et104WriteRequest request)
    {
        Optional(request.ContactorName,"ContactorName",50); Optional(request.TelNo,"TelNo",25);
        Optional(request.FaxNo,"FaxNo",25); Optional(request.Email,"Email",255);
        Optional(request.Address,"Address",500,multiline:true); Optional(request.BankNo,"BankNo",20); Optional(request.AcNo,"AcNo",50);
    }
    private static void Optional(string? value,string label,int maximum,bool multiline=false)
    {
        if (value?.Length>maximum || value?.Any(c=>char.IsControl(c) && !(multiline && (c is '\r' or '\n' or '\t')))==true)
            throw Rule(422,$"{label} 最多{maximum}字元且不可含無效控制字元。");
    }
    private static string Key(string? value,string label="ForwarderNo")
    {
        var key=value?.Trim(' ') ?? "";
        if (string.IsNullOrWhiteSpace(key) || key.Length>50 || key.Any(char.IsControl)) throw Rule(400,$"{label} 必填、最多50字元且不可含控制字元。");
        return key;
    }
    private static string? Search(string? value)
    {
        if (value is null) return null;
        if (value.Length>50 || value.Any(char.IsControl)) throw Rule(400,"ForwarderNo 查詢最多50字元且不可含控制字元。");
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
    { if (value!=0) throw Rule(409,"只有未簽核狀態0的貨運行可核准。"); }
    private static void RequireEditable(int? value)
    { if (value is not (0 or 2)) throw Rule(409,"只有未簽核0或已簽核2的貨運行可修改聯絡資料。"); }
    private static void ExpectedState(int? actual,int? expected)
    { if (actual!=expected) throw Conflict(); }
    private static void ExpectedStamp(decimal? actual,decimal? expected)
    { if (actual!=expected) throw Conflict(); }
    private static string? String(NpgsqlDataReader r,int i)=>r.IsDBNull(i)?null:r.GetString(i);
    private static DateTime? Date(NpgsqlDataReader r,int i)=>r.IsDBNull(i)?null:DateTime.SpecifyKind(r.GetDateTime(i),DateTimeKind.Utc);
    private static Et104ForwarderDto Map(NpgsqlDataReader r)=>new(r.GetString(0),String(r,1),String(r,2),String(r,3),String(r,4),String(r,5),r.GetString(6),String(r,7),
        r.IsDBNull(8)?null:r.GetInt32(8),String(r,9),Date(r,10),String(r,11),r.IsDBNull(12)?null:r.GetDecimal(12),String(r,13),Date(r,14));

    private static Et104ForwarderDto Returned(NpgsqlCommand command)
    { using var r=command.ExecuteReader(); if (!r.Read()) throw Conflict(); var row=Map(r); if (r.Read()) throw Conflict(); return row; }
    private static void Bind(NpgsqlCommand c,string no,Et104WriteRequest request,string actor)
    {
        Text(c,"no",no); Text(c,"contactor",request.ContactorName); Text(c,"tel",request.TelNo); Text(c,"fax",request.FaxNo);
        Text(c,"email",request.Email); Text(c,"address",request.Address); Text(c,"bank",request.BankNo); Text(c,"ac",request.AcNo); Text(c,"actor",actor);
    }
    private static void Text(NpgsqlCommand c,string name,string? value)=>c.Parameters.Add(name,NpgsqlDbType.Varchar).Value=(object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand c,string name,decimal? value)=>c.Parameters.Add(name,NpgsqlDbType.Numeric).Value=(object?)value ?? DBNull.Value;
    private static Et104RuleException Rule(int status,string detail)=>new(status,detail);
    private static Et104RuleException RegistryUnavailable()=>Rule(503,"貨運行相依來源或引用對應尚未完成核對；本次未變更資料。");
    private static Et104RuleException Alias()=>Rule(409,"ForwarderNo 存在大小寫／空白識別碼歧義，原始資料已保留。");
    private static Et104RuleException Conflict()=>Rule(409,"貨運行狀態或版本已變更，請重新查詢；本次未保存。");
}
