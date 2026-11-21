using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

// Candidate for the empty-Lot branch only. Existing read APIs remain unchanged.
public sealed class Wp109WriteRepository(NpgsqlDataSource source, Wp109WritePolicy policy)
{
    private const string Basis = "\"YMES\".tblwipswrbasis";
    private const string Detail = "\"YMES\".tblwipswrdetail";
    private const string Serial = "\"YMES\".tblsysserialno";
    private sealed record Group(Wp109RawHeader Header, IReadOnlyList<Wp109DetailDto> Details)
    {
        internal Wp109VersionedItemResult Result()
        {
            var h = Header;
            var dto = new Wp109SwrDto(h.SwrNo, h.SwrType, h.Status, h.CustomerNo, h.OpNo,
                h.DeviceNo, h.StartTime, h.EndTime, h.CustomerLotNo, h.MoNo, h.SwrDesc,
                h.Creator, h.CreateDate, new(h.LegacyAttachmentPath is not null and not "" ? "Y" : "N",
                    h.Blob?.Length, h.Blob is { Length: > 0 }));
            return new(dto, Details, Wp109WriteVersion.Compute(h, Details));
        }
    }

    public async Task<Wp109VersionedItemResult> VersionedItemAsync(string swrNo, CancellationToken token)
    {
        Key(swrNo); await using var c = await source.OpenConnectionAsync(token);
        await using var t = await c.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        await Execute(c, t, "SET TRANSACTION READ ONLY;", token);
        GuardGroup(c, t, write: false, token);
        var group = await ReadGroup(c, t, swrNo, update: false, token);
        var result = group.Result(); await t.CommitAsync(token); return result;
    }

    public Wp109NumberPreview Preview()
    {
        using var c = source.OpenConnection(); using var t = c.BeginTransaction(IsolationLevel.ReadCommitted);
        using (var ro = new NpgsqlCommand("SET TRANSACTION READ ONLY;", c, t)) ro.ExecuteNonQuery();
        Wp109WriteSchema.EnsureSerial(c, t, write: false);
        var now = BusinessNow(); var function = Function(now);
        using var query = new NpgsqlCommand($"SELECT serialno FROM {Serial} WHERE functionname=@function;", c, t);
        Text(query, "function", function); var value = query.ExecuteScalar();
        var next = value is null ? 1m : value is decimal number ? number : throw Unavailable("流水號內容不符合來源。");
        CheckCounter(next); t.Commit(); return new(Number(function, next), policy.BusinessTimeZone, false);
    }

    // Used before parsing the upload, and checked again in the mutation tx.
    public async Task<long> FileLimitAsync(CancellationToken token)
    {
        await using var c = await source.OpenConnectionAsync(token);
        await using var t = await c.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        await Execute(c, t, "SET TRANSACTION READ ONLY;", token);
        var limit = ReadFileLimit(c, t); await t.CommitAsync(token); return limit;
    }

    public async Task<Wp109WriterOptions> OptionsAsync(CancellationToken token)
    {
        _ = BusinessNow();
        await using var c = await source.OpenConnectionAsync(token);
        await using var t = await c.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        await Execute(c, t, "SET TRANSACTION READ ONLY;", token);
        var limit = ReadFileLimit(c, t); var reason = ReadAbortSetting(c, t);
        await t.CommitAsync(token); return new(limit, reason, policy.BusinessTimeZone);
    }

    internal async Task<Wp109VersionedItemResult> CreateAsync(Wp109CreateRequest request,
        Wp109IncomingAttachment? file, string actor, CancellationToken token)
    {
        ValidateFields(request); Actor(actor); Attachment(request.AttachmentAction, file, creating: true);
        await using var c = await source.OpenConnectionAsync(token);
        await using var t = await c.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        if (file is not null) CheckFile(file, ReadFileLimit(c, t));
        var mo = NormalizeMo(c, t, request.MoNo, null, creating: true);
        var parents = LockParents(c, t, request.CustomerNo!, request.OpNo!, null, null);
        GuardGroup(c, t, write: true, token);
        Wp109WriteSchema.EnsureSerial(c, t, write: true);
        var beforeWait = BusinessNow(); var function = Function(beforeWait);
        using (var ns = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:SYS:Serial:'||@function,0));", c, t))
        { Text(ns, "function", function); ns.ExecuteNonQuery(); }
        var now = BusinessNow();
        if (Function(now) != function) throw Conflict("配置前已跨營業日期，請重新開啟新增視窗。");
        var next = Allocate(c, t, function); var no = Number(function, next);
        AcquireSwr(c, t, no);
        // Native Detail has no FK. A new header may never adopt orphan lots
        // already stored under this generated raw number. Detail strong lock
        // is already held, so this fresh check also excludes later phantoms.
        using (var orphan = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {Detail} WHERE swrno=@no);", c, t))
        { Text(orphan, "no", no); if (orphan.ExecuteScalar() is true) throw Conflict("原始明細已存在，本次未接管。"); }
        using (var cmd = new NpgsqlCommand($"""
            INSERT INTO {Basis}(swrno,swrtype,status,customerno,opno,deviceno,starttime,endtime,
             customerlotno,mono,attachfile,swrdesc,creator,createdate,attachfilepath)
            VALUES(@no,@type,0,@customer,@op,@device,@start,@end,@customerLot,@mo,
             @file,@description,@actor,@now,@fileName);
            """, c, t))
        {
            BindFields(cmd, request, parents.Customer, parents.Op, mo); Text(cmd, "no", no);
            Text(cmd, "actor", actor); Time(cmd, "now", now); BindFile(cmd, file);
            if (await cmd.ExecuteNonQueryAsync(token) != 1) throw Conflict("建立結果不明，請重新查詢核對。");
        }
        var created = await ReadGroup(c, t, no, update: true, token);
        Empty(created); CheckStoredAttachment(created, file, keep: false, old: null);
        var saved = created.Result(); await t.CommitAsync(token); return saved;
    }

    internal async Task<Wp109VersionedItemResult> EditAsync(string swrNo, Wp109EditRequest request,
        Wp109IncomingAttachment? file, string actor, CancellationToken token)
    {
        Key(swrNo); ValidateFields(request); Version(request.Version); Actor(actor);
        Attachment(request.AttachmentAction, file, creating: false);
        await using var c = await source.OpenConnectionAsync(token);
        await using var t = await c.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        if (file is not null) CheckFile(file, ReadFileLimit(c, t));
        GuardGroup(c, t, write: false, token);
        // Planning read only: no child namespace/row lock is held before parents.
        string oldCustomer, oldOp;
        using (var planning = new NpgsqlCommand($"SELECT customerno,opno FROM {Basis} WHERE swrno=@no;", c, t))
        {
            Text(planning, "no", swrNo); using var r = planning.ExecuteReader();
            if (!r.Read()) throw NotFound(); oldCustomer = r.GetString(0); oldOp = r.GetString(1);
        }
        var parents = LockParents(c, t, request.CustomerNo!, request.OpNo!, oldCustomer, oldOp);
        GuardGroup(c, t, write: true, token); AcquireSwr(c, t, swrNo);
        var old = await ReadGroup(c, t, swrNo, update: true, token);
        if (!StringComparer.Ordinal.Equals(old.Header.CustomerNo, oldCustomer)
            || !StringComparer.Ordinal.Equals(old.Header.OpNo, oldOp)) throw Conflict("父引用已變更，請重新查詢。");
        CheckVersion(old, request.Version!);
        if (old.Header.Status != 0) throw Conflict("只有未凍結狀態0可編輯。");
        Empty(old);
        var customer = request.CustomerNo == oldCustomer ? oldCustomer : parents.Customer;
        var op = request.OpNo == oldOp ? oldOp : parents.Op;
        var mo = NormalizeMo(c, t, request.MoNo, old.Header.MoNo, creating: false);
        using (var cmd = new NpgsqlCommand($"""
            UPDATE {Basis} SET swrtype=@type,customerno=@customer,opno=@op,deviceno=@device,
             starttime=@start,endtime=@end,customerlotno=@customerLot,
             mono=@mo,
             swrdesc=@description,
             attachfile=CASE WHEN @keep THEN attachfile ELSE @file END,
             attachfilepath=CASE WHEN @keep THEN attachfilepath ELSE @fileName END
            WHERE swrno=@no AND status=0;
            """, c, t))
        {
            BindFields(cmd, request, customer, op, mo); Text(cmd, "no", swrNo);
            cmd.Parameters.Add("keep", NpgsqlDbType.Boolean).Value = request.AttachmentAction == "keep";
            BindFile(cmd, file);
            if (await cmd.ExecuteNonQueryAsync(token) != 1) throw Conflict("資料已變更，請重新查詢。");
        }
        var edited = await ReadGroup(c, t, swrNo, update: true, token);
        Empty(edited); CheckStoredAttachment(edited, file, request.AttachmentAction == "keep", old);
        var saved = edited.Result(); await t.CommitAsync(token); return saved;
    }

    public async Task<Wp109VersionedItemResult> ApproveAsync(string swrNo,
        Wp109ExpectedVersionRequest request, string actor, CancellationToken token)
    {
        Key(swrNo); Version(request.Version); Actor(actor);
        await using var c = await source.OpenConnectionAsync(token);
        await using var t = await c.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        GuardGroup(c, t, write: true, token); AcquireSwr(c, t, swrNo);
        var old = await ReadGroup(c, t, swrNo, update: true, token);
        CheckVersion(old, request.Version!);
        if (old.Header.Status != 0) throw Conflict("只有未凍結狀態0可核准。");
        Empty(old);
        if (old.Header.StartTime is { } start && (old.Header.EndTime is null || old.Header.EndTime.Value <= start))
            throw Invalid("結束時間必須晚於開始時間。");
        await ChangeStatus(c, t, swrNo, 0, 2, token);
        var saved = (await ReadGroup(c, t, swrNo, update: true, token)).Result();
        await t.CommitAsync(token); return saved;
    }

    public async Task<Wp109DeleteResult> DeleteAsync(string swrNo,
        Wp109ExpectedVersionRequest request, string actor, CancellationToken token)
    {
        Key(swrNo); Version(request.Version); Actor(actor);
        await using var c = await source.OpenConnectionAsync(token);
        await using var t = await c.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        GuardGroup(c, t, write: true, token); AcquireSwr(c, t, swrNo);
        var old = await ReadGroup(c, t, swrNo, update: true, token);
        CheckVersion(old, request.Version!);
        if (old.Header.Status is not (0 or 99)) throw Conflict("直接刪除只接受狀態0或99。");
        Empty(old);
        await DeleteGroup(c, t, old, token); await t.CommitAsync(token); return new(swrNo, 1, 0);
    }

    public async Task<Wp109VoidResult> VoidAsync(string swrNo, Wp109VoidRequest request,
        string actor, CancellationToken token)
    {
        Key(swrNo); Version(request.Version); Actor(actor);
        Value(request.AbortReason, int.MaxValue, required: false, "作廢原因", nullable: false);
        await using var c = await source.OpenConnectionAsync(token);
        await using var t = await c.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        // Read the real parameter; missing provider is not the fallback0 branch.
        _ = ReadAbortSetting(c, t);
        using var log = Wp109WriteSchema.PrepareEventInsert(c, t);
        GuardGroup(c, t, write: true, token); AcquireSwr(c, t, swrNo);
        var old = await ReadGroup(c, t, swrNo, update: true, token);
        CheckVersion(old, request.Version!);
        if (old.Header.Status is not (0 or 2 or 99)) throw Conflict("此狀態沒有可執行的作廢流程。");
        Empty(old);
        Wp109VersionedItemResult? saved = null;
        if (old.Header.Status == 2)
        {
            await ChangeStatus(c, t, swrNo, 2, 99, token);
            saved = (await ReadGroup(c, t, swrNo, update: true, token)).Result();
        }
        else if (old.Header.Status is 0 or 99) await DeleteGroup(c, t, old, token);
        else throw Conflict("此狀態沒有可執行的作廢流程。");
        log.Parameters["actor"].Value = actor; log.Parameters["at"].Value = BusinessNow();
        log.Parameters["description"].Value = "AbortSWRBasis:" + swrNo + "," + request.AbortReason;
        log.Parameters["no"].Value = swrNo;
        if (await log.ExecuteNonQueryAsync(token) != 1) throw Unavailable("稽核紀錄未完成，本單未提交。");
        await t.CommitAsync(token);
        return new(swrNo, saved is null ? "deleted" : "voided", saved, saved is null ? 1 : 0, 0);
    }

    private static async Task<Group> ReadGroup(NpgsqlConnection c, NpgsqlTransaction t,
        string no, bool update, CancellationToken token)
    {
        using var cmd = new NpgsqlCommand($"""
            SELECT a.swrno,a.swrtype,a.status,a.customerno,a.opno,a.deviceno,a.starttime,a.endtime,
             a.customerlotno,a.mono,a.swrdesc,a.creator,a.createdate,a.attachfilepath,
             (SELECT coalesce(jsonb_agg(jsonb_build_array(d.swrno,d.lotno) ORDER BY d.lotno COLLATE "C"),'[]'::jsonb)::text
              FROM {Detail} d WHERE d.swrno=a.swrno),octet_length(a.attachfile)::bigint,a.attachfile
            FROM {Basis} a WHERE a.swrno=@no
            """ + (update ? " FOR UPDATE OF a;" : ";"), c, t);
        Text(cmd, "no", no);
        using var cancel = token.Register(() => { try { cmd.Cancel(); } catch (NpgsqlException) { } catch (ObjectDisposedException) { } });
        await using var r = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, token);
        if (!await r.ReadAsync(token)) throw NotFound();
        var rawNo = r.GetString(0); var type = decimal.ToInt32(r.GetDecimal(1)); var status = decimal.ToInt32(r.GetDecimal(2));
        var customer = r.GetString(3); var op = r.GetString(4); var device = Optional(r, 5);
        var start = Wall(r, 6); var end = Wall(r, 7); var lot = Optional(r, 8); var mo = Optional(r, 9);
        var description = r.GetString(10); var creator = Optional(r, 11); var created = Wall(r, 12); var path = Optional(r, 13);
        using var document = JsonDocument.Parse(r.GetString(14));
        var details = new List<Wp109DetailDto>();
        foreach (var row in document.RootElement.EnumerateArray())
        {
            if (row.GetArrayLength() != 2 || row[0].ValueKind != JsonValueKind.String || row[1].ValueKind != JsonValueKind.String)
                throw Unavailable("原始明細形狀不符合來源。");
            details.Add(new(row[0].GetString()!, row[1].GetString()!));
        }
        Wp109BlobDigest? digest = null;
        if (!await r.IsDBNullAsync(15, token))
        {
            var expectedLength = r.GetInt64(15);
            await using var content = await r.GetStreamAsync(16, token);
            digest = await Wp109WriteVersion.HashBlobAsync(content, token);
            if (digest.Length != expectedLength) throw Unavailable("附件讀取未完成。");
        }
        return new(new(rawNo, type, status, customer, op, device, start, end, lot, mo, digest,
            description, creator, created, path), details);
    }

    private static (string Customer, string Op) LockParents(NpgsqlConnection c, NpgsqlTransaction t,
        string requestedCustomer, string requestedOp, string? oldCustomer, string? oldOp)
    {
        Wp109WriteSchema.EnsureParentKeys(c, t);
        var customers = Normalize(c, t, new[] { requestedCustomer, oldCustomer }.OfType<string>().ToArray());
        foreach (var key in customers)
        {
            using var ns = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:ENT:Customer:'||@key,0));", c, t);
            Text(ns, "key", key); ns.ExecuteNonQuery();
        }
        var customer = LockParent(c, t, "tblentcustomerbasis", "customerno", customers, requestedCustomer);
        var ops = Normalize(c, t, new[] { requestedOp, oldOp }.OfType<string>().ToArray());
        var op = LockParent(c, t, "tblopbasis", "opno", ops, requestedOp);
        return (customer, op);
    }

    private static string[] Normalize(NpgsqlConnection c, NpgsqlTransaction t, string[] keys)
    {
        using var cmd = new NpgsqlCommand("SELECT value FROM (SELECT DISTINCT lower(btrim(v)) AS value FROM unnest(@keys::varchar[]) q(v)) x ORDER BY value COLLATE \"C\";", c, t);
        cmd.Parameters.Add("keys", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = keys;
        using var r = cmd.ExecuteReader(); var values = new List<string>(); while (r.Read()) values.Add(r.GetString(0)); return values.ToArray();
    }

    private static string LockParent(NpgsqlConnection c, NpgsqlTransaction t, string table,
        string column, string[] keys, string requested)
    {
        // table/column are private fixed literals, never a request identifier.
        string wanted;
        using (var norm = new NpgsqlCommand("SELECT lower(btrim(@key));", c, t))
        { Text(norm, "key", requested); wanted = (string)norm.ExecuteScalar()!; }
        using var cmd = new NpgsqlCommand($"SELECT {column},lower(btrim({column})) FROM \"YMES\".{table} WHERE lower(btrim({column}))=ANY(@keys) ORDER BY lower(btrim({column})) COLLATE \"C\",{column} COLLATE \"C\" FOR SHARE;", c, t);
        cmd.Parameters.Add("keys", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = keys;
        using var r = cmd.ExecuteReader(); var found = new Dictionary<string, string>(StringComparer.Ordinal);
        while (r.Read()) if (!found.TryAdd(r.GetString(1), r.GetString(0))) throw Conflict("父識別碼有多個原始表示，資料已保留。");
        if (!found.TryGetValue(wanted, out var result)) throw Conflict("找不到指定的客戶或作業站。");
        return result;
    }

    private static decimal Allocate(NpgsqlConnection c, NpgsqlTransaction t, string function)
    {
        using var read = new NpgsqlCommand($"SELECT serialno FROM {Serial} WHERE functionname=@function FOR UPDATE;", c, t);
        Text(read, "function", function); var old = read.ExecuteScalar();
        var next = old is null ? 1m : old is decimal value ? value : throw Unavailable("流水號來源值無效。");
        CheckCounter(next);
        using var write = new NpgsqlCommand(old is null
            ? $"INSERT INTO {Serial}(functionname,serialno) VALUES(@function,@next);"
            : $"UPDATE {Serial} SET serialno=@next WHERE functionname=@function AND serialno=@old;", c, t);
        Text(write, "function", function); Numeric(write, "next", next + 1);
        if (old is not null) Numeric(write, "old", next);
        if (write.ExecuteNonQuery() != 1) throw Conflict("流水號已變更，請重新查詢。"); return next;
    }

    private static void AcquireSwr(NpgsqlConnection c, NpgsqlTransaction t, string no)
    {
        using var cmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:WIP:SWR:'||@no,0));", c, t);
        Text(cmd, "no", no); cmd.ExecuteNonQuery();
    }

    private static void GuardGroup(NpgsqlConnection c, NpgsqlTransaction t, bool write, CancellationToken token)
    { token.ThrowIfCancellationRequested(); Wp109WriteSchema.EnsureGroup(c, t, write); token.ThrowIfCancellationRequested(); }
    private static void CheckVersion(Group old, string expected)
    { if (!Wp109WriteVersion.Matches(expected, old.Result().Version)) throw Conflict("資料已變更，請重新查詢核對。"); }
    private static void Empty(Group group)
    { if (group.Details.Count != 0) throw Unavailable("非空Lot的原生來源尚未就緒，本單已保留。"); }
    private static void Version(string? version)
    { if (!Wp109WriteVersion.Valid(version)) throw Bad("請提供完整的原始版本。"); }

    private static async Task DeleteGroup(NpgsqlConnection c, NpgsqlTransaction t, Group old, CancellationToken token)
    {
        using (var detail = new NpgsqlCommand($"DELETE FROM {Detail} WHERE swrno=@no;", c, t))
        { Text(detail, "no", old.Header.SwrNo); if (await detail.ExecuteNonQueryAsync(token) != 0) throw Conflict("明細已變更，資料已保留。"); }
        using var cmd = new NpgsqlCommand($"DELETE FROM {Basis} WHERE swrno=@no AND status=@status;", c, t);
        Text(cmd, "no", old.Header.SwrNo); Numeric(cmd, "status", old.Header.Status);
        if (await cmd.ExecuteNonQueryAsync(token) != 1) throw Conflict("刪除結果不符，資料已保留。");
    }

    private static async Task ChangeStatus(NpgsqlConnection c, NpgsqlTransaction t, string no, int expected, int status, CancellationToken token)
    {
        using var cmd = new NpgsqlCommand($"UPDATE {Basis} SET status=@status WHERE swrno=@no AND status=@expected;", c, t);
        Text(cmd, "no", no); Numeric(cmd, "status", status); Numeric(cmd, "expected", expected);
        if (await cmd.ExecuteNonQueryAsync(token) != 1) throw Conflict("狀態已變更，請重新查詢。");
    }

    private static string? Parameter(NpgsqlConnection c, NpgsqlTransaction t, string no)
    {
        Wp109WriteSchema.EnsureParameter(c, t);
        // SELECT-only provider. A row FOR SHARE would need UPDATE privilege;
        // do not expand a read-only parameter grant to pretend settings freeze.
        using var cmd = new NpgsqlCommand("SELECT parametervalue FROM \"YMES\".tblsysparameter WHERE parameterno=@no;", c, t);
        Text(cmd, "no", no); var value = cmd.ExecuteScalar();
        return value is null ? null : value is string text ? text : throw Unavailable("系統參數格式不符合來源。");
    }

    private static long ReadFileLimit(NpgsqlConnection c, NpgsqlTransaction t)
    {
        var text = Parameter(c, t, "FileSizeLimit");
        if (text is null) return 10L * 1_048_576;
        if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            || value < 0 || value > long.MaxValue / 1_048_576m) throw Unavailable("附件大小參數無效。");
        return decimal.ToInt64(decimal.Truncate(value * 1_048_576m));
    }

    private static bool ReadAbortSetting(NpgsqlConnection c, NpgsqlTransaction t)
    {
        var value = Parameter(c, t, "InputAbortLog")?.Trim(' ');
        return value switch { null or "0" => false, "1" => true, _ => throw Unavailable("作廢原因參數無效。") };
    }

    private DateTime BusinessNow()
    {
        if (policy.BusinessTimeZone != "Asia/Taipei") throw Unavailable("營業時區尚未設定。");
        try
        {
            var wall = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(policy.BusinessTimeZone)).DateTime;
            return DateTime.SpecifyKind(wall.AddTicks(-(wall.Ticks % 10)), DateTimeKind.Unspecified);
        }
        catch (TimeZoneNotFoundException) { throw Unavailable("營業時區尚未就緒。"); }
        catch (InvalidTimeZoneException) { throw Unavailable("營業時區設定無效。"); }
    }

    private static string Function(DateTime value) => "SWR" + value.ToString("yyMMdd", CultureInfo.InvariantCulture);
    private static string Number(string function, decimal next) => function + next.ToString("000", CultureInfo.InvariantCulture);
    private static void CheckCounter(decimal value)
    { if (value != decimal.Truncate(value) || value < 1 || value > 999) throw Conflict("本日流水號無法配置或已用盡，未建立資料。"); }

    private static void ValidateFields(Wp109FieldsRequest r)
    {
        if (r.SwrType is not (0 or 1)) throw Invalid("SWRType只接受0或1。");
        Value(r.CustomerNo, 50, true, "CustomerNo"); Value(r.OpNo, 20, true, "OPNo");
        Value(r.DeviceNo, 50, false, "DeviceNo"); Value(r.CustomerLotNo, 50, false, "CustomerLotNo");
        Value(r.MoNo, 50, false, "MONo"); Value(r.SwrDesc, 400, true, "SWRDescription");
        if (r.LotNos is null) throw Bad("LotNos須明確提供陣列。");
        foreach (var lot in r.LotNos) Value(lot, 50, true, "LotNo");
        if (r.LotNos.Length != 0) throw Unavailable("非空Lot的原生來源尚未就緒。");
        foreach (var value in new[] { r.StartTime, r.EndTime })
            if (value is { } time && (time.Kind != DateTimeKind.Unspecified || time.Ticks % 10 != 0))
                throw Invalid("排程時間須為無時區的原始時間，最多六位小數。");
        if (r.StartTime.HasValue != r.EndTime.HasValue
            || r.StartTime is { } start && r.EndTime is { } end && end <= start)
            throw Invalid("開始及結束須一起設定，結束必須較晚。");
    }

    private static void Value(string? value, int max, bool required, string label, bool nullable = true)
    {
        if (value is null) { if (required || !nullable) throw Invalid(label + "必須存在。"); return; }
        try { _ = new UTF8Encoding(false, true).GetByteCount(value); }
        catch (EncoderFallbackException) { throw Invalid(label + "含無效Unicode。"); }
        if (value.Contains('\0') || value.EnumerateRunes().Count() > max || required && value.Trim(' ').Length == 0)
            throw Invalid(label + "不符合原生欄位限制。");
    }
    private static void Key(string no)
    {
        try { Value(no, 20, true, "SWRNo"); }
        catch (Wp109WriteRuleException ex) { throw Bad(ex.Message); }
    }
    private static void Actor(string actor) => Value(actor, 10, true, "登入使用者");
    private static void Attachment(string? action, Wp109IncomingAttachment? file, bool creating)
    {
        if (creating ? action is not ("none" or "replace") : action is not ("keep" or "clear" or "replace"))
            throw Bad("附件動作無效。");
        if ((action == "replace") != (file is not null)) throw Bad("附件動作與實際檔案不相符。");
    }
    private static void CheckFile(Wp109IncomingAttachment file, long limit)
    { if (file.Length > limit) throw new Wp109WriteRuleException(413, "附件超過目前系統大小上限。"); }
    private static void CheckStoredAttachment(Group saved, Wp109IncomingAttachment? file, bool keep, Group? old)
    {
        var expected = keep ? old!.Header.Blob : file is null ? null : new Wp109BlobDigest(file.Length, file.Sha256);
        var path = keep ? old!.Header.LegacyAttachmentPath : file is null ? null : "attachment.pdf";
        if (!Equals(saved.Header.Blob, expected) || !StringComparer.Ordinal.Equals(saved.Header.LegacyAttachmentPath, path))
            throw Conflict("附件保存結果不符，本次未提交。");
    }
    private static void BindFile(NpgsqlCommand cmd, Wp109IncomingAttachment? file)
    {
        if (file is not null) file.Content.Position = 0;
        cmd.Parameters.Add("file", NpgsqlDbType.Bytea).Value = file is null ? DBNull.Value : file.Content;
        Text(cmd, "fileName", file is null ? null : "attachment.pdf");
    }
    private static string? NormalizeMo(NpgsqlConnection c, NpgsqlTransaction t, string? value, string? old, bool creating)
    {
        if (!creating && StringComparer.Ordinal.Equals(value, old)) return old;
        if (value is null) return null;
        using var cmd = new NpgsqlCommand("SELECT upper(@mo);", c, t); Text(cmd, "mo", value);
        var result = (string)cmd.ExecuteScalar()!; Value(result, 50, false, "MONo"); return result;
    }
    private static void BindFields(NpgsqlCommand cmd, Wp109FieldsRequest r, string customer, string op, string? mo)
    {
        Numeric(cmd, "type", r.SwrType!.Value); Text(cmd, "customer", customer); Text(cmd, "op", op);
        Text(cmd, "device", r.DeviceNo); Time(cmd, "start", r.StartTime); Time(cmd, "end", r.EndTime);
        Text(cmd, "customerLot", r.CustomerLotNo); Text(cmd, "mo", mo); Text(cmd, "description", r.SwrDesc);
    }
    private static void Text(NpgsqlCommand cmd, string key, string? value) => cmd.Parameters.Add(key, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;
    private static void Numeric(NpgsqlCommand cmd, string key, decimal value) => cmd.Parameters.Add(key, NpgsqlDbType.Numeric).Value = value;
    private static void Time(NpgsqlCommand cmd, string key, DateTime? value) => cmd.Parameters.Add(key, NpgsqlDbType.Timestamp).Value = (object?)value ?? DBNull.Value;
    private static string? Optional(NpgsqlDataReader r, int n) => r.IsDBNull(n) ? null : r.GetString(n);
    private static DateTime? Wall(NpgsqlDataReader r, int n) => r.IsDBNull(n) ? null : DateTime.SpecifyKind(r.GetDateTime(n), DateTimeKind.Unspecified);
    private static async Task Execute(NpgsqlConnection c, NpgsqlTransaction t, string sql, CancellationToken token)
    { using var cmd = new NpgsqlCommand(sql, c, t); await cmd.ExecuteNonQueryAsync(token); }
    private static Wp109WriteRuleException Bad(string text) => new(400, text);
    private static Wp109WriteRuleException Invalid(string text) => new(422, text);
    private static Wp109WriteRuleException Conflict(string text) => new(409, text);
    private static Wp109WriteRuleException Unavailable(string text) => new(503, text);
    private static Wp109WriteRuleException NotFound() => new(404, "找不到指定的原始SWRNo。");
}
