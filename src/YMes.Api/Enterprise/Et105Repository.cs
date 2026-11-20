using System.Data;
using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Enterprise;

public sealed class Et105Repository(NpgsqlDataSource dataSource)
{
    private const decimal MaxStamp = 99_999_999_999m;
    private const string Table = "\"YMES\".tblenttakerbasis";
    private const string Columns = "takerno,contactorname,telno,faxno,email,address,bankno,issuestate::int,creator,createdate,datastamp,reviser,revisedate";
    private static readonly string[] RelationNames = ["tblenttakerbasis"];
    private static readonly string[] ReferenceNames = ["takerno", "taker", "takerassigned"];

    public IReadOnlyList<Et105TakerDto> List(string? takerNo, string? issueState)
    {
        var search = Search(takerNo); var state = FilterState(issueState);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        EnsureRegistry(connection, transaction);
        using var command = new NpgsqlCommand($"""
            WITH rows AS (
              SELECT *,count(*) OVER(PARTITION BY lower(btrim(takerno))) AS aliases FROM {Table})
            SELECT {Columns},aliases FROM rows
            WHERE (@state IS NULL OR issuestate=@state)
              AND (@search IS NULL OR position(lower(@search) in lower(takerno))>0)
            ORDER BY lower(btrim(takerno)) COLLATE "C",takerno COLLATE "C";
            """, connection, transaction);
        Text(command, "search", search); Number(command, "state", state);
        var items = new List<Et105TakerDto>();
        using (var reader = command.ExecuteReader()) while (reader.Read())
        {
            if (reader.GetInt64(13) > 1) throw Alias();
            items.Add(Map(reader));
        }
        transaction.Commit(); return items;
    }

    public Et105TakerDto Get(string takerNo)
    {
        var no = Key(takerNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        EnsureRegistry(connection, transaction);
        var row = Read(connection, transaction, no);
        transaction.Commit(); return row;
    }

    public Et105TakerDto Create(Et105WriteRequest request, string actor)
    {
        var raw = Key(request.TakerNo); actor = Actor(actor); ValidateFields(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        // PostgreSQL is the authority for new-key case conversion. Validate the
        // result again because a Unicode uppercase can expand the source key.
        var no = NewKey(connection, transaction, raw);
        Acquire(connection, transaction, no);
        EnsureRegistry(connection, transaction);
        using (var duplicate = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {Table} WHERE lower(btrim(takerno))=lower(btrim(@no)));", connection, transaction))
        { Text(duplicate, "no", no); if (duplicate.ExecuteScalar() is true) throw Rule(409, "取貨公司識別碼已存在或有大小寫／空白別名，不能覆寫來源列。"); }
        using var command = new NpgsqlCommand($"""
            INSERT INTO {Table}(takerno,contactorname,telno,faxno,email,address,bankno,issuestate,creator,createdate,datastamp,reviser,revisedate)
            VALUES(@no,@contactor,@tel,@fax,@email,@address,@bank,0,@actor,now() AT TIME ZONE 'UTC',0,NULL,NULL)
            RETURNING {Columns};
            """, connection, transaction);
        Bind(command, no, request, actor);
        var result = Returned(command); transaction.Commit(); return result;
    }

    public Et105TakerDto Update(string takerNo, Et105UpdateRequest request, string actor)
    {
        var no = Key(takerNo); actor = Actor(actor); ValidateStamp(request.DataStamp); ValidateFields(request);
        if (string.IsNullOrWhiteSpace(request.TakerNo) || request.TakerNo.Length>50 || request.TakerNo.Any(char.IsControl))
            throw Rule(400, "TakerNo 必填、最多50字元且不可含控制字元。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Acquire(connection, transaction, no); EnsureRegistry(connection, transaction);
        var current = Read(connection, transaction, no, update: true);
        KnownState(current.IssueState); ExpectedStamp(current.DataStamp, request.DataStamp);
        RequireDraft(current.IssueState); Incrementable(current.DataStamp);
        if (!string.Equals(request.TakerNo, current.TakerNo, StringComparison.Ordinal))
            throw Rule(422, "修改時不可變更 TakerNo，請保留原始識別碼（含大小寫與空白）。");
        using var command = new NpgsqlCommand($"""
            UPDATE {Table}
            SET contactorname=@contactor,telno=@tel,faxno=@fax,email=@email,address=@address,bankno=@bank,
                reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE takerno=@no AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {Columns};
            """, connection, transaction);
        // Never normalize/re-key an existing raw identifier or rewrite its
        // creation audit/unknown extension columns during a metadata edit.
        Bind(command, current.TakerNo, request, actor);
        Number(command, "state", current.IssueState); Number(command, "stamp", request.DataStamp);
        var result = Returned(command); transaction.Commit(); return result;
    }

    public Et105TakerDto Approve(string takerNo, Et105ExpectedRequest request, string actor)
    {
        var no = Key(takerNo); actor = Actor(actor); ValidateStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Acquire(connection, transaction, no); EnsureRegistry(connection, transaction);
        var current = Read(connection, transaction, no, update: true);
        KnownState(current.IssueState); ExpectedState(current.IssueState, request.ExpectedIssueState);
        ExpectedStamp(current.DataStamp, request.DataStamp); RequireDraft(current.IssueState); Incrementable(current.DataStamp);
        // Native TAKER BASIS no-Job direct approval is usable only when no relevant actual
        // catalog candidate exists. A named/pattern candidate is not a guessed
        // empty Job or a successful notification; provider delivery is required.
        if (WorkflowMayExist(connection, transaction))
            throw Rule(503, "已偵測到簽核來源，但尚未提供此功能的簽核服務；本次未核准或建立簽核單。");
        var result = ChangeState(connection, transaction, current, 2, actor);
        transaction.Commit(); return result;
    }

    public Et105DeleteResult Delete(string takerNo, Et105ExpectedRequest request, string actor)
    {
        var no = Key(takerNo); actor = Actor(actor); ValidateStamp(request.DataStamp);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        Acquire(connection, transaction, no); EnsureRegistry(connection, transaction);
        var current = Read(connection, transaction, no, update: true);
        KnownState(current.IssueState); ExpectedState(current.IssueState, request.ExpectedIssueState);
        ExpectedStamp(current.DataStamp, request.DataStamp);
        if (current.IssueState == 1) throw Rule(409, "簽核中的取貨公司不可刪除。");
        if (current.IssueState == 2)
        {
            // Existing references do not prevent native2→-1 retirement.
            Incrementable(current.DataStamp);
            var saved = ChangeState(connection, transaction, current, -1, actor);
            transaction.Commit(); return new(false, saved);
        }
        // Explicit YMES integrity policy, not a native FK/cascade claim:
        // EnsureRegistry has already rejected every unreviewed actual Taker
        // reference. The current reviewed registry has no external table.
        // Future ET106 writers must take this same Taker namespace, then
        // an approved parent SHARE through commit before inserting references.
        // ShipAddr is absent today; do not query it or invent a false result.
        using (var command = new NpgsqlCommand($"DELETE FROM {Table} WHERE takerno=@no AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction))
        {
            Text(command, "no", current.TakerNo); Number(command, "state", request.ExpectedIssueState); Number(command, "stamp", request.DataStamp);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
        }
        // Hard delete performs no bump, so a matching positive maximum is valid.
        transaction.Commit(); return new(true, null);
    }

    private static Et105TakerDto ChangeState(NpgsqlConnection connection, NpgsqlTransaction transaction, Et105TakerDto current, int next, string actor)
    {
        using var command = new NpgsqlCommand($"""
            UPDATE {Table} SET issuestate=@next,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE takerno=@no AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {Columns};
            """, connection, transaction);
        Text(command, "no", current.TakerNo); Text(command, "actor", actor);
        Number(command, "next", next); Number(command, "state", current.IssueState); Number(command, "stamp", current.DataStamp);
        return Returned(command);
    }

    private static Et105TakerDto Read(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, bool update = false)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM {Table} WHERE lower(btrim(takerno))=lower(btrim(@no)) ORDER BY takerno COLLATE \"C\"" + (update ? " FOR UPDATE;" : ";"), connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到指定取貨公司。");
        var row = Map(reader); if (reader.Read()) throw Alias(); return row;
    }

    private static string NewKey(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT upper(btrim(@new));", connection, transaction);
        Text(command, "new", no);
        var normalized = command.ExecuteScalar() as string;
        if (normalized is null || string.IsNullOrWhiteSpace(normalized) || normalized.Length>50 || normalized.Any(char.IsControl))
            throw Rule(400, "TakerNo 經資料庫大寫處理後必須為1–50字元且不可含控制字元。");
        return normalized;
    }

    private static void Acquire(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:ENT:Taker:' || lower(btrim(@no)),0));", connection, transaction);
        Text(command, "no", no); command.ExecuteNonQuery();
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
            if (found.Count!=1) throw RegistryUnavailable();
        }
        var expectedReferences = new HashSet<(string Table,string Column,bool NotNull)>
        {
            // The audited registry contains only this actual owner key.
            // Native future ShipAddr.TakerNo is absent. Any newly appearing
            // reference must be reviewed/activated, never queried by guess.
            ("tblenttakerbasis","takerno",true)
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
            ["takerno"]=(1,"character varying(50)",true),["contactorname"]=(2,"character varying(50)",false),
            ["telno"]=(3,"character varying(25)",false),["faxno"]=(4,"character varying(25)",false),
            ["email"]=(5,"character varying(255)",false),["address"]=(6,"character varying(500)",false),
            ["bankno"]=(7,"character varying(20)",false),["issuestate"]=(8,"numeric(1,0)",false),
            ["creator"]=(9,"character varying(10)",false),["createdate"]=(10,"timestamp without time zone",false),
            ["datastamp"]=(11,"numeric(11,0)",false),["reviser"]=(12,"character varying(10)",false),
            ["revisedate"]=(13,"timestamp without time zone",false)
        };
        using (var command = new NpgsqlCommand("""
            SELECT a.attname,a.attnum::int,format_type(a.atttypid,a.atttypmod),a.attnotnull,a.attidentity::text,a.attgenerated::text
            FROM pg_catalog.pg_attribute a WHERE a.attrelid='"YMES".tblenttakerbasis'::regclass
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
        // Verify the actual owner read grant with a zero-row probe and keep
        // its relation ACCESS SHARE lock until commit. There are no reviewed
        // external reference providers to query; new catalog mappings fail503.
        using var probe = new NpgsqlCommand($"SELECT {Columns} FROM {Table} WHERE false;", connection, transaction);
        probe.ExecuteNonQuery();
    }

    private static void ValidateFields(Et105WriteRequest request)
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
        if (string.IsNullOrWhiteSpace(key) || key.Length>50 || key.Any(char.IsControl)) throw Rule(400,"TakerNo 必填、最多50字元且不可含控制字元。");
        return key;
    }
    private static string? Search(string? value)
    {
        if (value is null) return null;
        if (value.Length>50 || value.Any(char.IsControl)) throw Rule(400,"TakerNo 查詢最多50字元且不可含控制字元。");
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
    { if (value!=0) throw Rule(409,"只有未簽核狀態0的取貨公司可修改或核准。"); }
    private static void ExpectedState(int? actual,int? expected)
    { if (actual!=expected) throw Conflict(); }
    private static void ExpectedStamp(decimal? actual,decimal? expected)
    { if (actual!=expected) throw Conflict(); }
    private static string? String(NpgsqlDataReader r,int i)=>r.IsDBNull(i)?null:r.GetString(i);
    private static DateTime? Date(NpgsqlDataReader r,int i)=>r.IsDBNull(i)?null:DateTime.SpecifyKind(r.GetDateTime(i),DateTimeKind.Utc);
    private static Et105TakerDto Map(NpgsqlDataReader r)=>new(r.GetString(0),String(r,1),String(r,2),String(r,3),String(r,4),String(r,5),String(r,6),
        r.IsDBNull(7)?null:r.GetInt32(7),String(r,8),Date(r,9),r.IsDBNull(10)?null:r.GetDecimal(10),String(r,11),Date(r,12));
    private static Et105TakerDto Returned(NpgsqlCommand command)
    { using var r=command.ExecuteReader(); if (!r.Read()) throw Conflict(); var row=Map(r); if (r.Read()) throw Conflict(); return row; }
    private static void Bind(NpgsqlCommand c,string no,Et105WriteRequest request,string actor)
    {
        Text(c,"no",no); Text(c,"contactor",request.ContactorName); Text(c,"tel",request.TelNo); Text(c,"fax",request.FaxNo);
        Text(c,"email",request.Email); Text(c,"address",request.Address); Text(c,"bank",request.BankNo); Text(c,"actor",actor);
    }
    private static void Text(NpgsqlCommand c,string name,string? value)=>c.Parameters.Add(name,NpgsqlDbType.Varchar).Value=(object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand c,string name,decimal? value)=>c.Parameters.Add(name,NpgsqlDbType.Numeric).Value=(object?)value ?? DBNull.Value;
    private static Et105RuleException Rule(int status,string detail)=>new(status,detail);
    private static Et105RuleException RegistryUnavailable()=>Rule(503,"取貨公司相依來源或引用對應尚未完成核對；本次未變更資料。");
    private static Et105RuleException Alias()=>Rule(409,"TakerNo 存在大小寫／空白識別碼歧義，原始資料已保留。");
    private static Et105RuleException Conflict()=>Rule(409,"取貨公司狀態或版本已變更，請重新查詢；本次未保存。");
}
