using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed partial class Qc201Repository(NpgsqlDataSource dataSource, IConfiguration configuration, ILogger<Qc201Repository> logger)
{
    private const decimal MaxStamp = 99_999_999_999m;
    private const string ItemColumns = "qcitemno,qcitemname,lotisvalid::int,componentcollect::int,planno,ruleno,formname,description,limitlotnum::int,issamplesizeinteger::int,qcclass,mclassno,issuestate::int,datastamp,creator,createdate,reviser,revisedate,executionfile";
    private static readonly string[] ChildTables = ["tblqcitemparameter", "tblqcitemfactor", "tblqcitemreasonmap", "tblqcsampleplanchange", "tblopqcitem"];
    private static readonly string[] IssueTables = ["tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail"];

    public IReadOnlyList<Qc201ItemDto> List(int? issueState, string? search)
    {
        if (issueState is not (null or -1 or 0 or 1 or 2)) throw Rule(400, "IssueState 僅允許 -1、0、1、2；省略時讀全部原始狀態。");
        var text = Code(search, "查詢文字", 25, false);
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand($"SELECT {ItemColumns} FROM \"YMES\".tblqcitemforattributes WHERE (@state IS NULL OR issuestate=@state) AND (@search IS NULL OR @search='' OR position(lower(@search) in lower(qcitemno))>0) ORDER BY lower(qcitemno),qcitemno;", connection);
        Number(command, "state", issueState); Text(command, "search", text);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc201ItemDto>();
        while (reader.Read()) rows.Add(MapItem(reader));
        return rows;
    }

    public Qc201ItemDto Get(string qcItemNo)
    {
        using var connection = dataSource.OpenConnection();
        return ReadItem(connection, null, Key(qcItemNo), false);
    }

    public Qc201ItemDto Create(Qc201ItemWriteRequest request, string actor)
    {
        var value = Normalize(request); actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        QcItemIdentityGuard.Acquire(connection, transaction, [value.QcItemNo]);
        QcItemIdentityGuard.EnsureAvailable(connection, transaction, value.QcItemNo);
        // INSERT owns the new Item before acquiring any lookup locks. A failed
        // lookup rolls this row back; no commit-then-GET ghost is possible.
        InsertItem(connection, transaction, value, actor);
        EnsureLookups(connection, transaction, value, null);
        CanonicalizeNewItem(connection, transaction, value);
        var result = ReadItem(connection, transaction, value.QcItemNo, false);
        transaction.Commit(); Audit("Create", result, actor); return result;
    }

    public Qc201ItemDto Update(string qcItemNo, Qc201UpdateRequest request, string actor)
    {
        var value = Normalize(request); actor = Actor(actor); var no = Key(qcItemNo);
        if (!Same(no, value.QcItemNo)) throw Rule(400, "更新時不可變更 QCItemNo。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var current = LockItem(connection, transaction, no);
        Expected(current, request.IssueState, request.DataStamp);
        if (current.IssueState == 2) throw Rule(503, "已核准主檔的 IssueControl 可改欄位來源尚未交付，目前保留原資料。");
        RequireDraft(current); GuardIncrement(current.DataStamp);
        value.QcItemNo = current.QcItemNo;
        EnsureLookups(connection, transaction, value, current);
        using var command = new NpgsqlCommand($"""
            UPDATE "YMES".tblqcitemforattributes
            SET qcitemname=@name,lotisvalid=@lot,componentcollect=@component,planno=@plan,ruleno=@rule,
                formname=@form,description=@description,limitlotnum=@limit,issamplesizeinteger=@integer,
                qcclass=@class,mclassno=@mclass,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',
                datastamp=COALESCE(datastamp,0)+1
            WHERE qcitemno=@no AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {ItemColumns};
            """, connection, transaction);
        BindWrite(command, value, actor); Number(command, "state", current.IssueState); Number(command, "stamp", current.DataStamp);
        var result = ReadReturned(command);
        transaction.Commit(); Audit("Update", result, actor); return result;
    }

    public Qc201CopyPreview CopyPreview(string sourceNo, Qc201CopyWriteRequest request)
    {
        var value = Normalize(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        QcItemIdentityGuard.Acquire(connection, transaction, [sourceNo, value.QcItemNo]);
        QcItemIdentityGuard.EnsureAttributeOwner(connection, transaction, sourceNo);
        var source = ReadItem(connection, transaction, Key(sourceNo), true);
        EnsureChildOwners(connection, transaction, source.QcItemNo);
        Expected(source, request.IssueState, request.DataStamp);
        if (source.ComponentCollect != value.ComponentCollect) throw Rule(400, "複製時 ComponentCollect 不可變更。");
        var plan = LockCopyPlans(connection, transaction, source, value, request);
        var preview = BuildCopyPreview(connection, transaction, source, request, plan);
        transaction.Commit(); return preview;
    }

    public Qc201ItemDto Copy(string sourceNo, Qc201CopyWriteRequest request, string actor)
    {
        var value = Normalize(request); actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        QcItemIdentityGuard.Acquire(connection, transaction, [sourceNo, value.QcItemNo]);
        QcItemIdentityGuard.EnsureAttributeOwner(connection, transaction, sourceNo);
        QcItemIdentityGuard.EnsureAvailable(connection, transaction, value.QcItemNo);
        var source = ReadItem(connection, transaction, Key(sourceNo), true);
        EnsureChildOwners(connection, transaction, source.QcItemNo);
        Expected(source, request.IssueState, request.DataStamp);
        if (source.IssueState is null or 1) throw Rule(409, "簽核中或狀態未知的來源不可複製。");
        if (source.ComponentCollect != value.ComponentCollect) throw Rule(400, "複製時 ComponentCollect 不可變更。");
        InsertItem(connection, transaction, value, actor);
        var plan = LockCopyPlans(connection, transaction, source, value, request);
        EnsureLookups(connection, transaction, value, null, plan);
        CanonicalizeNewItem(connection, transaction, value);
        var preview = BuildCopyPreview(connection, transaction, source, request, plan);
        RequireVersion(request.SourceVersion);
        if (!string.Equals(request.SourceVersion, preview.SourceVersion, StringComparison.Ordinal)) throw Conflict();
        if (request.CopyParameters && preview.ParametersDiffer && request.ParameterSource is not ("item" or "plan"))
            throw Rule(422, "來源項目與目標抽樣計劃參數不同，請明確選擇 ParameterSource=item 或 plan；尚未建立新項目。");
        if (request.ParameterSource is not (null or "item" or "plan")) throw Rule(400, "ParameterSource 只允許 item 或 plan。");
        CopyChildren(connection, transaction, source, value.QcItemNo, request, preview, actor);
        var result = ReadItem(connection, transaction, value.QcItemNo, false);
        transaction.Commit(); Audit("Copy", result, actor); return result;
    }

    public Qc201ItemDto Approve(string qcItemNo, Qc201ApproveRequest request, string actor)
    {
        actor = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var current = LockItem(connection, transaction, Key(qcItemNo));
        Expected(current, request.IssueState, request.DataStamp); RequireDraft(current); GuardIncrement(current.DataStamp);
        var value = Normalize(FromItem(current));
        EnsureLookups(connection, transaction, value, current);
        var factors = ReadFactors(connection, transaction, current.QcItemNo);
        var reasons = ReadReasonMaps(connection, transaction, current.QcItemNo);
        if (factors.Count == 0 || reasons.Count == 0) throw Rule(422, "至少需要一筆 Factor 定義及一筆 Reason 對應才能核准。");
        LockCatalogs(connection, transaction, factors.Select(x => x.FactorNo));
        LockReasons(connection, transaction, reasons.Select(x => x.ReasonNo));
        if (IssueTables.Any(table => TableExists(connection, transaction, table)))
            throw Rule(503, "YMES 已存在 Issue/Job 表；QC201 尚未接入正式簽核服務，主檔未變更。");
        using var command = new NpgsqlCommand($"""
            UPDATE "YMES".tblqcitemforattributes SET issuestate=2,reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
            WHERE qcitemno=@no AND issuestate IS NOT DISTINCT FROM @state AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {ItemColumns};
            """, connection, transaction);
        BindExpected(command, current); Text(command, "actor", actor);
        var result = ReadReturned(command);
        transaction.Commit(); Audit("ApproveWithoutJob", result, actor); return result;
    }

    public void Delete(string qcItemNo, Qc201DeleteRequest request, string actor)
    {
        _ = Actor(actor);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var current = LockItem(connection, transaction, Key(qcItemNo));
        Expected(current, request.IssueState, request.DataStamp);
        if (current.IssueState is not (0 or 2 or -1)) throw Rule(409, "此狀態不可刪除或停用。");
        // The source explicitly checks WIP before BOTH physical delete and 2→-1.
        // No verified WIP query/provider exists yet. Do not guess a table, skip
        // absent tables, or modify even the soft-delete branch before this check.
        throw Rule(503, "計數品管項目刪除／停用所需的原生 WIP 使用檢查尚未交付真實表映射與查詢規則；主檔及所有明細均未變更。");
    }

    public Qc201OptionDto GetOptions(int? componentCollect = 0)
    {
        using var connection = dataSource.OpenConnection();
        var availability = new Dictionary<string, Qc201Availability>();
        var missing = new List<string>();
        IReadOnlyList<string>? Read(string key, string table, string column)
        {
            if (!TableExists(connection, null, table)) { availability[key] = new(false, $"尚未建立 {table}。"); missing.Add(table); return null; }
            var values = ReadApprovedCodes(connection, table, column);
            availability[key] = new(true, null); return values;
        }
        var plans = componentCollect == 0 ? Read("samplingPlans", "tblqcsamplingplan", "planno") : null;
        if (componentCollect != 0) { availability["samplingPlans"] = new(false, "Wafer 或未知 ComponentCollect 的抽樣計劃 provider 尚未提供。"); missing.Add("Wafer/unknown sampling-plan provider"); }
        var rules = Read("attributeRules", "tblqcattrulebasis", "ruleno");
        var classes = Read("mClasses", "tblqcmclassbasis", "mclassno");
        availability["interfaces"] = new(false, "Special Form 介面目錄尚未核實。");
        availability["reasonTypes"] = new(false, "QC201 ReasonType 的正式 ValueList 尚未提供；真實已核准 ReasonNo 對應可獨立維護。");
        availability["simulation"] = new(false, "原生 Simulation 核心尚未交付。");
        availability["delete"] = new(false, "原生 WIP 使用檢查 provider 尚未交付，包含停用。");
        missing.AddRange(["Special Form interface directory", "QC201 ReasonType ValueList", "Simulation Core", "QC201 WIP usage provider"]);
        var mode = (configuration["YMES_QC201_SYSTEM_MODE"] ?? "SPC").Trim().ToUpperInvariant();
        return new(plans, rules, classes, null, mode is "ZKT" or "KYT", mode != "SPK", mode != "SPK", availability, missing);
    }

    public IReadOnlyList<Qc201ReasonDto> ApprovedReasons()
    {
        using var connection = dataSource.OpenConnection(); EnsureTable(connection, null, "tblqcreasonbasis");
        using var command = new NpgsqlCommand("SELECT reasonno,reasonname,reasontype::int,reasonlevel::int,issuestate::int FROM \"YMES\".tblqcreasonbasis WHERE issuestate=2 ORDER BY lower(reasonno),reasonno;", connection);
        using var reader = command.ExecuteReader(); var rows = new List<Qc201ReasonDto>();
        while (reader.Read()) rows.Add(new(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4)));
        return rows;
    }

    public IReadOnlyList<Qc201ValueOption> ReasonTypeOptions() => throw Rule(503, "QC201 ReasonType 的精確 ValueList key、有效值與標籤尚未交付；不可以其他功能或 DEV 代碼代替。");
    public IReadOnlyList<string> Interfaces() => throw Rule(503, "Special Form 介面目錄尚未核實。");

    public IReadOnlyList<Qc201FactorChoice> FactorChoices()
    {
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblsysmesparameter");
        using var command = new NpgsqlCommand("""
            WITH catalog AS (
                SELECT parameterno,parametername,parametertype::int,issuestate,
                       count(*) OVER(PARTITION BY lower(btrim(parameterno))) AS aliascount
                FROM "YMES".tblsysmesparameter
            )
            SELECT parameterno,parametername,parametertype,aliascount FROM catalog
            WHERE issuestate=2 ORDER BY lower(parameterno),parameterno;
            """, connection);
        using var reader = command.ExecuteReader();
        var rows = new List<Qc201FactorChoice>();
        while (reader.Read())
        {
            if (reader.GetInt64(3) != 1) throw Rule(409, "MES ParameterNo 存在大小寫／空白歧義，請先整理真實目錄。");
            var type = reader.GetInt32(2);
            rows.Add(new(reader.GetString(0), reader.GetString(1), type, type == 2));
        }
        return rows;
    }

    public Qc201FactorReference FactorReference(string factorNo)
    {
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var factor = ReadCatalog(connection, transaction, Code(factorNo, "FactorNo", 30, true)!, true);
        if (factor.ParameterType != 2) throw Rule(503, "此真實 MES Parameter 需要 Table／Function reference provider，目前未提供；不回假值或空選項。");
        transaction.Commit(); return new(factor.ParameterNo, factor.ParameterType, true, null);
    }

    public IReadOnlyList<Qc201OperationRow> ApprovedOperationChoices(int qcType, int lotIsValid)
    {
        if (qcType != 0 || lotIsValid is not (0 or 1)) throw Rule(400, "QCType 必須為0，LotIsValid 必須為0或1。");
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("SELECT opno,opname FROM \"YMES\".tblopbasis WHERE issuestate=2 ORDER BY lower(opno),opno;", connection);
        using var reader = command.ExecuteReader(); var rows = new List<Qc201OperationRow>();
        while (reader.Read()) rows.Add(new() { OpNo = reader.GetString(0), OpName = String(reader, 1), QcItemType = 0 });
        return rows;
    }

    public IReadOnlyList<Dictionary<string, object>> Simulation(string qcItemNo, Qc201SimulationRequest request)
    { _ = Get(qcItemNo); throw Rule(503, "正式抽樣 Simulation 原 Core 尚未交付；不回近似或 DEV 假計算。"); }

    private static Qc201ItemDto LockItem(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        QcItemIdentityGuard.Acquire(connection, transaction, [no]);
        QcItemIdentityGuard.EnsureAttributeOwner(connection, transaction, no);
        var row = ReadItem(connection, transaction, no, true);
        EnsureChildOwners(connection, transaction, row.QcItemNo);
        return row;
    }

    private static Qc201ItemDto ReadItem(NpgsqlConnection connection, NpgsqlTransaction? transaction, string no, bool forUpdate, bool forShare = false)
    {
        using var command = new NpgsqlCommand($"SELECT {ItemColumns} FROM \"YMES\".tblqcitemforattributes WHERE lower(btrim(qcitemno))=lower(btrim(@no)) ORDER BY lower(qcitemno),qcitemno" + (forUpdate ? " FOR UPDATE;" : forShare ? " FOR SHARE;" : ";"), connection, transaction);
        Text(command, "no", no); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到計數品管項目。");
        var row = MapItem(reader);
        if (reader.Read()) throw Rule(409, "QCItemNo 存在大小寫／空白歧義，無法安全選取。");
        return row;
    }

    private static void InsertItem(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc201ItemWriteRequest value, string actor)
    {
        using var command = new NpgsqlCommand("""
            INSERT INTO "YMES".tblqcitemforattributes(qcitemno,qcitemname,lotisvalid,componentcollect,planno,ruleno,formname,description,limitlotnum,issamplesizeinteger,qcclass,mclassno,issuestate,datastamp,creator,createdate,reviser,revisedate)
            VALUES(@no,@name,@lot,@component,@plan,@rule,@form,@description,@limit,@integer,@class,@mclass,0,0,@actor,now() AT TIME ZONE 'UTC',NULL,NULL);
            """, connection, transaction);
        BindWrite(command, value, actor); command.ExecuteNonQuery();
    }

    private static void CanonicalizeNewItem(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc201ItemWriteRequest value)
    {
        using var command = new NpgsqlCommand("UPDATE \"YMES\".tblqcitemforattributes SET planno=@plan,ruleno=@rule,mclassno=@mclass WHERE qcitemno=@no;", connection, transaction);
        Text(command, "plan", value.PlanNo); Text(command, "rule", value.RuleNo); Text(command, "mclass", value.MClassNo); Text(command, "no", value.QcItemNo);
        if (command.ExecuteNonQuery() != 1) throw Conflict();
    }

    private static QcSamplingPlanReference.GeneralPlan EnsureLookups(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc201ItemWriteRequest value, Qc201ItemDto? existing, QcSamplingPlanReference.GeneralPlan? lockedPlan = null)
    {
        if (!string.IsNullOrEmpty(value.FormName) && !string.Equals(value.FormName, existing?.FormName, StringComparison.Ordinal))
            throw Rule(503, "Special Form 介面目錄尚未核實；可保留既有相同值，不能新增或變更未驗證的介面。");
        var plan = lockedPlan ?? QcSamplingPlanReference.ReadGeneral(connection, transaction, value.PlanNo!, value.ComponentCollect, 2, Rule);
        value.PlanNo = plan.PlanNo;
        // Plan→attribute rule→class is the same lookup order for all paths.
        if (!string.IsNullOrEmpty(value.RuleNo)) value.RuleNo = ApprovedCode(connection, transaction, "tblqcattrulebasis", "ruleno", value.RuleNo);
        if (!string.IsNullOrEmpty(value.MClassNo)) value.MClassNo = ApprovedCode(connection, transaction, "tblqcmclassbasis", "mclassno", value.MClassNo);
        return plan;
    }

    private static string ApprovedCode(NpgsqlConnection connection, NpgsqlTransaction transaction, string table, string column, string value)
    {
        EnsureTable(connection, transaction, table);
        using var command = new NpgsqlCommand($"SELECT {column},issuestate::int FROM \"YMES\".{table} WHERE lower(btrim({column}))=lower(btrim(@value)) ORDER BY {column} FOR SHARE;", connection, transaction);
        Text(command, "value", value); using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(422, "指定主檔不存在。");
        var canonical = reader.GetString(0); var state = Int(reader, 1);
        if (reader.Read()) throw Rule(409, "指定主檔存在大小寫／空白歧義。");
        if (state != 2) throw Rule(422, "指定主檔尚未核准。");
        return canonical;
    }

    private static IReadOnlyList<string> ReadApprovedCodes(NpgsqlConnection connection, string table, string column)
    {
        using var command = new NpgsqlCommand($"SELECT {column} FROM \"YMES\".{table} WHERE issuestate=2 ORDER BY lower({column}),{column};", connection);
        using var reader = command.ExecuteReader(); var rows = new List<string>();
        while (reader.Read()) rows.Add(reader.GetString(0)); return rows;
    }

    internal static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@name AND c.relkind IN ('r','p'));", connection, transaction);
        Text(command, "name", table); return command.ExecuteScalar() is true;
    }
    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    { if (!TableExists(connection, transaction, table)) throw Rule(503, $"YMES 尚未建立真實相依表 {table}；不能當作空資料。"); }

    private Qc201ItemWriteRequest Normalize(Qc201ItemWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var component = request.ComponentCollect;
        if (component is null && !string.IsNullOrWhiteSpace(request.PlanNo)) throw Rule(503, "ComponentCollect 為NULL，無法判定非空 PlanNo 的來源；不可當成一般計劃。");
        if (component is not (0 or 1) || request.LotIsValid is not (0 or 1) || request.IsSampleSizeInteger is not (0 or 1)) throw Rule(400, "LotIsValid、ComponentCollect、IsSampleSizeInteger 必須明確為0或1。");
        if (request.LimitLotNum is < 1 or > 99999 or null) throw Rule(400, "LimitLotNum 必須為1到99999的整數。");
        var mode = (configuration["YMES_QC201_SYSTEM_MODE"] ?? "SPC").Trim().ToUpperInvariant();
        var qcClass = Code(request.QcClass, "QCClass", 30, false);
        if (mode is "ZKT" or "KYT" && qcClass is not ("SPC" or "IPQC")) throw Rule(400, "ZKT/KYT 必須指定 SPC 或 IPQC。");
        return new() { QcItemNo = Key(request.QcItemNo).ToUpperInvariant(), QcItemName = TextValue(request.QcItemName, "QCItemName", 500, true),
            LotIsValid = request.LotIsValid, ComponentCollect = component, PlanNo = Code(request.PlanNo, "PlanNo", 25, true),
            RuleNo = Code(request.RuleNo, "RuleNo", 20, false), FormName = TextValue(request.FormName, "FormName", 50, false),
            Description = TextValue(request.Description, "Description", 255, false), LimitLotNum = request.LimitLotNum,
            IsSampleSizeInteger = request.IsSampleSizeInteger, QcClass = qcClass, MClassNo = Code(request.MClassNo, "MClassNo", 30, false) };
    }

    private static Qc201ItemWriteRequest FromItem(Qc201ItemDto row) => new() { QcItemNo=row.QcItemNo,QcItemName=row.QcItemName,LotIsValid=row.LotIsValid,ComponentCollect=row.ComponentCollect,PlanNo=row.PlanNo,RuleNo=row.RuleNo,FormName=row.FormName,Description=row.Description,LimitLotNum=row.LimitLotNum,IsSampleSizeInteger=row.IsSampleSizeInteger,QcClass=row.QcClass,MClassNo=row.MClassNo };
    private static void Expected(Qc201ItemDto current, int? state, decimal? stamp)
    { ValidateStamp(stamp); if (current.IssueState != state || current.DataStamp != stamp) throw Conflict(); }
    private static void RequireDraft(Qc201ItemDto row)
    { if (row.IssueState != 0) throw Rule(409, "本批 YMES 保守規則僅允許未簽核(state0)修改；NULL不視為0。"); }
    private static void ValidateStamp(decimal? stamp)
    { if (stamp is { } value && (value != decimal.Truncate(value) || value < 0 || value > MaxStamp)) throw Rule(400, "DataStamp 必須為NULL或numeric(11,0)非負整數。"); }
    private static void GuardIncrement(decimal? stamp)
    { ValidateStamp(stamp); if (stamp >= MaxStamp) throw Rule(409, "DataStamp 已達上限，資料未變更。"); }
    private static void RequireVersion(string? version)
    { if (version is null || version.Length != 64 || version.Any(ch => !Uri.IsHexDigit(ch))) throw Rule(400, "必須提供本次GET/preview取得的完整Version。"); }
    private static string Fingerprint(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    private static Qc201ItemDto ReadReturned(NpgsqlCommand command)
    { using var reader = command.ExecuteReader(); if (!reader.Read()) throw Conflict(); var row = MapItem(reader); if (reader.Read()) throw Conflict(); return row; }
    private static Qc201ItemDto MapItem(NpgsqlDataReader r) => new(r.GetString(0),String(r,1),Int(r,2),Int(r,3),String(r,4),String(r,5),String(r,6),String(r,7),Int(r,8),Int(r,9),String(r,10),String(r,11),Int(r,12),Decimal(r,13),String(r,14),Date(r,15),String(r,16),Date(r,17),String(r,18));
    private static string? String(NpgsqlDataReader reader,int index) => reader.IsDBNull(index)?null:reader.GetString(index);
    private static int? Int(NpgsqlDataReader reader,int index) => reader.IsDBNull(index)?null:reader.GetInt32(index);
    private static decimal? Decimal(NpgsqlDataReader reader,int index) => reader.IsDBNull(index)?null:reader.GetDecimal(index);
    private static DateTime? Date(NpgsqlDataReader reader,int index) => reader.IsDBNull(index)?null:DateTime.SpecifyKind(reader.GetDateTime(index),DateTimeKind.Utc);
    private static string Key(string? value) => Code(value,"QCItemNo",25,true)!;
    private static string? Code(string? value,string field,int max,bool required)
    { var text=value?.Trim(' '); if ((required && string.IsNullOrWhiteSpace(text)) || text?.Length>max || text?.Any(char.IsControl)==true) throw Rule(400,$"{field}為必要文字或超出{max}字元／含控制字元。"); return text; }
    private static string? TextValue(string? value,string field,int max,bool required)
    { if ((required && string.IsNullOrWhiteSpace(value)) || value?.Length>max || value?.Any(ch=>char.IsControl(ch)&&ch is not '\r' and not '\n' and not '\t')==true) throw Rule(400,$"{field}為必要文字或超出{max}字元／含無效字元。"); return value; }
    private static string Actor(string actor) => Code(actor,"登入帳號",10,true)!;
    private static bool Same(string? left,string? right) => string.Equals(left?.Trim(' '),right?.Trim(' '),StringComparison.OrdinalIgnoreCase);
    private static void Text(NpgsqlCommand command,string name,string? value) => command.Parameters.Add(name,NpgsqlDbType.Varchar).Value=(object?)value??DBNull.Value;
    private static void Number(NpgsqlCommand command,string name,decimal? value) => command.Parameters.Add(name,NpgsqlDbType.Numeric).Value=(object?)value??DBNull.Value;
    private static void BindExpected(NpgsqlCommand command,Qc201ItemDto row) { Text(command,"no",row.QcItemNo);Number(command,"state",row.IssueState);Number(command,"stamp",row.DataStamp); }
    private static void BindWrite(NpgsqlCommand command,Qc201ItemWriteRequest value,string actor)
    { Text(command,"no",value.QcItemNo);Text(command,"name",value.QcItemName);Number(command,"lot",value.LotIsValid);Number(command,"component",value.ComponentCollect);Text(command,"plan",value.PlanNo);Text(command,"rule",value.RuleNo);Text(command,"form",value.FormName);Text(command,"description",value.Description);Number(command,"limit",value.LimitLotNum);Number(command,"integer",value.IsSampleSizeInteger);Text(command,"class",value.QcClass);Text(command,"mclass",value.MClassNo);Text(command,"actor",actor); }
    private void Audit(string action,Qc201ItemDto row,string actor) => logger.LogInformation("QC201 {Action} {QcItemNo} by {Actor}; state {State}; stamp {Stamp}",action,row.QcItemNo,actor,row.IssueState,row.DataStamp);
    private static Qc201RuleException Conflict() => Rule(409,"資料或明細已被其他操作更新，請重新讀取；本次未保存。");
    private static Qc201RuleException Rule(int status,string message) => new(status,message);
}
