using System.Data;
using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc103Repository(NpgsqlDataSource dataSource)
{
    private const string Columns = "planno,planname,plantype::int,functionname,description,creator,createdate,issuestate::int,sqtycanbbiggerthenpqty::int,datastamp,reviser,revisedate";
    public static IReadOnlyList<Qc103PlanTypeOption> PlanTypes { get; } = [
        new(0, "Fixed Sample Size"), new(1, "Fixed Sample Rate"), new(2, "AQL"), new(3, "User Define Function"),
        new(4, "No Definition"), new(5, "Sampling Table"), new(6, "Sampling 1916"), new(7, "AQL")];

    public static IReadOnlyList<Qc103ParameterDto> ParameterSpecs(int type) => type switch
    {
        0 => [new("PopulationQty", null, 0), new("SampleQty", null, 1)],
        1 => [new("PopulationQty", null, 0), new("SampleQty", null, 0), new("SampleRate", null, 1)],
        2 or 7 => [new("AcceptQty", null, 0), new("AQLType", null, 1), new("PopulationQty", null, 0), new("QCLevel", null, 1), new("RejectQty", null, 0), new("SampleQty", null, 0), new("AQL", null, 1)],
        4 => [new("PopulationQty", null, 2), new("SampleQty", null, 2)],
        5 => [new("SamplingTable", null, 1), new("PopulationQty", null, 0), new("SampleQty", null, 0), new("RejectQty", null, 0)],
        // Verified source form defaults; SYS ElementValue's conditional override
        // still needs its real provider and does not imply an implemented engine.
        6 => [new("SamplingType", null, 1), new("VLevel", null, 1), new("PopulationQty", null, 0), new("SampleQty", null, 0), new("KValue", null, 0), new("FValue", null, 0), new("RejectQty", null, 0)],
        _ => []
    };

    public IReadOnlyList<Qc103PlanDto> List(int? issueState, int? componentCollect, string? searchFieldName, string? searchFieldValue)
    {
        if (issueState is not null and not (-1 or 0 or 1 or 2)) throw Rule(400, "IssueState 僅允許 -1、0、1、2。");
        QcSamplingPlanReference.RequireGeneral(componentCollect ?? 0, Rule, invalidStatus: 400);
        if (!string.IsNullOrWhiteSpace(searchFieldName) && !string.Equals(searchFieldName.Trim(), "PlanNo", StringComparison.OrdinalIgnoreCase)) throw Rule(400, "查詢欄位只允許 PlanNo。");
        var search = OptionalText(searchFieldValue, "PlanNo 查詢", 25) ?? "";
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcsamplingplan");
        var sql = $"SELECT {Columns} FROM \"YMES\".tblqcsamplingplan WHERE (@search='' OR position(lower(@search) in lower(planno))>0)" +
            (issueState is null ? "" : " AND issuestate=@state") + " ORDER BY planno;";
        using var command = new NpgsqlCommand(sql, connection);
        Text(command, "search", search, 25);
        if (issueState is { } state) Number(command, "state", state);
        var rows = new List<Qc103PlanDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) rows.Add(Read(reader));
        return rows;
    }

    public Qc103PlanDto Get(string planNo)
    {
        var no = Key(planNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        EnsureTables(connection, transaction);
        var row = ReadPlan(connection, transaction, no, false);
        var result = row with { Parameters = ReadParameters(connection, transaction, row) };
        transaction.Commit();
        return result;
    }

    public IReadOnlyList<Qc103ParameterDto> Parameters(string planNo, int? issueState, int? componentCollect = null)
    {
        if (issueState is not null and not (-1 or 0 or 1 or 2)) throw Rule(400, "IssueState 僅允許 -1、0、1、2。");
        // Omission is the published QC103 general-plan contract. A known Wafer
        // request must carry its namespace and never fall through by PlanNo.
        QcSamplingPlanReference.RequireGeneral(componentCollect ?? 0, Rule, invalidStatus: 400);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var row = QcSamplingPlanReference.ReadGeneral(connection, transaction, Key(planNo), componentCollect ?? 0, issueState, Rule, stateMismatchStatus: 409, missingStatus: 404);
        var specs = ParameterSpecs(row.PlanType);
        // Preserve existing QC301 lookup's empty-string parameter contract.
        var result = row.Parameters.Select(value => new Qc103ParameterDto(value.ParameterNo, value.ParameterValue ?? "",
            specs.FirstOrDefault(spec => string.Equals(spec.ParameterNo, value.ParameterNo, StringComparison.OrdinalIgnoreCase))?.ShowFlag)).ToArray();
        transaction.Commit(); return result;
    }

    public IReadOnlyList<string> ParameterOptions(string parameterNo, string? samplingType)
    {
        var parameter = RequiredText(parameterNo, "ParameterNo", 20);
        var key = parameter.ToUpperInvariant();
        if (key is "AQLTYPE" or "QCLEVEL" or "AQL") throw Rule(503, "原 SYS ElementValue 抽樣選項來源尚未遷移；目前可維護文字值，但不提供猜測的 AQL 選項。");
        if (key is not ("SAMPLINGTABLE" or "SAMPLINGTYPE" or "VLEVEL")) return [];
        using var connection = dataSource.OpenConnection();
        var table = key == "SAMPLINGTABLE" ? "tblqcsamplingtable" : "tblqcsampling1916";
        EnsureTable(connection, null, table);
        var column = key switch { "SAMPLINGTABLE" => "samplingtable", "SAMPLINGTYPE" => "samplingtype", _ => "slevel" };
        var type = OptionalText(samplingType, "SamplingType", 25);
        if (type is not null and not ("0" or "1")) throw Rule(400, "SamplingType 僅允許 0 或 1。");
        // Identifiers come only from the fixed allowlist above.
        var filter = key == "VLEVEL" && type is not null ? " AND samplingtype=@type" : "";
        using var command = new NpgsqlCommand($"SELECT DISTINCT {column} FROM \"YMES\".{table} WHERE {column} IS NOT NULL AND btrim({column})<>''{filter} ORDER BY {column};", connection);
        if (filter.Length > 0) Text(command, "type", type, 25);
        var values = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) values.Add(reader.GetString(0));
        return values;
    }

    public Qc103PlanDto Create(Qc103PlanWriteRequest request, string actor)
    {
        actor = RequiredText(actor, "登入帳號", 10);
        var value = Normalize(request, creating: true);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTables(connection, transaction);
            using (var duplicate = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblqcsamplingplan WHERE lower(planno)=lower(@no);", connection, transaction))
            {
                Text(duplicate, "no", value.No, 25);
                if (duplicate.ExecuteScalar() is not null) throw Rule(409, "PlanNo 已存在。");
            }
            ValidateSamplingTable(connection, transaction, value.Type, value.Parameters);
            using var command = new NpgsqlCommand($"""
                INSERT INTO "YMES".tblqcsamplingplan(planno,planname,plantype,functionname,description,sqtycanbbiggerthenpqty,issuestate,creator,createdate)
                VALUES(@no,@name,@type,@function,@description,@allow,0,@actor,now()) RETURNING {Columns};
                """, connection, transaction);
            Bind(command, value, actor);
            var result = ReadReturned(command);
            ReplaceParameters(connection, transaction, result.PlanNo, value.Parameters);
            result = result with { Parameters = ReadParameters(connection, transaction, result) };
            transaction.Commit(); return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation) { Rollback(transaction); throw Rule(409, "PlanNo 或參數編號已存在。"); }
        catch { Rollback(transaction); throw; }
    }

    public Qc103PlanDto Update(string planNo, Qc103PlanWriteRequest request, int? state, decimal? stamp, string actor)
    {
        actor = RequiredText(actor, "登入帳號", 10);
        var no = Key(planNo);
        if (!string.IsNullOrWhiteSpace(request.PlanNo) && Key(request.PlanNo) != no) throw Rule(400, "PlanNo 不可修改。");
        var value = Normalize(request, creating: false) with { No = no };
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTables(connection, transaction);
            var current = ReadPlan(connection, transaction, no, true);
            Expected(current, state, stamp);
            if (current.IssueState is not (0 or 2)) throw Rule(409, "只有未簽核或已簽核的抽樣計劃可修改。");
            ValidateSamplingTable(connection, transaction, value.Type, value.Parameters);
            value = value with { No = current.PlanNo };
            using var command = new NpgsqlCommand($"""
                UPDATE "YMES".tblqcsamplingplan SET planname=@name,plantype=@type,functionname=@function,description=@description,
                    sqtycanbbiggerthenpqty=@allow,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE planno=@no AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {Columns};
                """, connection, transaction);
            Bind(command, value, actor); Number(command, "state", current.IssueState); Number(command, "stamp", stamp);
            var result = ReadReturned(command);
            ReplaceParameters(connection, transaction, result.PlanNo, value.Parameters);
            result = result with { Parameters = ReadParameters(connection, transaction, result) };
            transaction.Commit(); return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc103PlanDto Delete(string planNo, int? state, decimal? stamp, string actor) => Change(planNo, state, stamp, actor, false);
    public Qc103PlanDto Approve(string planNo, int? state, decimal? stamp, string actor) => Change(planNo, state, stamp, actor, true);

    private Qc103PlanDto Change(string planNo, int? state, decimal? stamp, string actor, bool approve)
    {
        actor = RequiredText(actor, "登入帳號", 10);
        var no = Key(planNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTables(connection, transaction);
            var current = ReadPlan(connection, transaction, no, true);
            Expected(current, state, stamp);
            current = current with { Parameters = ReadParameters(connection, transaction, current) };
            if (approve)
            {
                if (current.IssueState != 0) throw Rule(409, "只有未簽核的抽樣計劃可以核准。");
                ValidateParameters(current.PlanType, current.Parameters);
                if (current.PlanType is 2 or 3 or 6 or 7) throw Rule(503, "此 PlanType 的原 Core／SYS 選項或函式驗證尚未遷移，不能略過檢查直接核准；主檔未變更。");
                ValidateSamplingTable(connection, transaction, current.PlanType, current.Parameters);
                if (new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" }.Any(table => TableExists(connection, transaction, table)))
                    throw Rule(503, "YMES 已存在 Issue/Job 表；QC103 尚未接入共用簽核服務，資料未變更。");
            }
            else if (current.IssueState == 1) throw Rule(409, "簽核中的抽樣計劃不可刪除。");
            if (!approve && current.IssueState is 0 or -1)
            {
                ReplaceParameters(connection, transaction, current.PlanNo, []);
                using var deletion = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcsamplingplan WHERE planno=@no AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction);
                ExpectedParameters(deletion, current);
                if (deletion.ExecuteNonQuery() != 1) throw Conflict();
                transaction.Commit(); return current;
            }
            if (!approve && current.IssueState != 2) throw Rule(409, "目前抽樣計劃狀態不允許刪除。");
            using var command = new NpgsqlCommand($"""
                UPDATE "YMES".tblqcsamplingplan SET issuestate=@next,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE planno=@no AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {Columns};
                """, connection, transaction);
            ExpectedParameters(command, current); Text(command, "actor", actor, 10); Number(command, "next", approve ? 2 : -1);
            var result = ReadReturned(command) with { Parameters = current.Parameters };
            transaction.Commit(); return result;
        }
        catch { Rollback(transaction); throw; }
    }

    private sealed record PlanValue(string No, string Name, int Type, string? Function, string? Description, int Allow, IReadOnlyList<Qc103ParameterDto> Parameters);
    private static PlanValue Normalize(Qc103PlanWriteRequest request, bool creating)
    {
        var no = creating ? Key(request.PlanNo) : "";
        var name = RequiredText(request.PlanName, "PlanName", 50);
        if (request.PlanType is < 0 or > 7) throw Rule(400, "PlanType 僅允許 0–7。");
        if (request.SQtyCanBBiggerThenPQty is not (0 or 1)) throw Rule(400, "抽樣數可超過母體旗標僅允許 0 或 1。");
        var function = OptionalText(request.FunctionName, "FunctionName", 50);
        if (request.PlanType == 3 && function is null) throw Rule(400, "User Define Function 必須提供 FunctionName。");
        if (request.Parameters is null || request.Parameters.Count > 256) throw Rule(400, "必須提供 parameters 陣列，最多 256 個參數。");
        var specs = ParameterSpecs(request.PlanType);
        var values = request.Parameters.Select(item =>
        {
            if (item is null) throw Rule(400, "參數列不可為 null。");
            var key = RequiredText(item.ParameterNo, "ParameterNo", 20);
            var spec = specs.FirstOrDefault(value => string.Equals(value.ParameterNo, key, StringComparison.OrdinalIgnoreCase));
            return new Qc103ParameterDto(spec?.ParameterNo ?? key, OptionalText(item.ParameterValue, key, 255), spec?.ShowFlag);
        }).ToArray();
        if (values.Select(item => item.ParameterNo).Distinct(StringComparer.OrdinalIgnoreCase).Count() != values.Length) throw Rule(409, "ParameterNo 不可重複（不區分大小寫）。");
        ValidateParameters(request.PlanType, values);
        return new(no, name, request.PlanType, function, OptionalText(request.Description, "Description", 255, multiline: true), request.SQtyCanBBiggerThenPQty, values);
    }

    private static void ValidateParameters(int type, IReadOnlyList<Qc103ParameterDto> values)
    {
        foreach (var spec in ParameterSpecs(type))
        {
            var value = values.FirstOrDefault(item => string.Equals(item.ParameterNo, spec.ParameterNo, StringComparison.OrdinalIgnoreCase));
            if (value is null) throw Rule(400, $"缺少必要參數 {spec.ParameterNo}。");
            if (spec.ShowFlag == 1 && string.IsNullOrWhiteSpace(value.ParameterValue)) throw Rule(400, $"{spec.ParameterNo} 為必填。");
        }
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value.ParameterValue)) continue;
            if (string.Equals(value.ParameterNo, "SampleQty", StringComparison.OrdinalIgnoreCase) &&
                (!decimal.TryParse(value.ParameterValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var quantity) || quantity < 0)) throw Rule(400, "SampleQty 必須為非負數。");
            if (string.Equals(value.ParameterNo, "SampleRate", StringComparison.OrdinalIgnoreCase) &&
                (!decimal.TryParse(value.ParameterValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate) || rate < 0 || rate > 1)) throw Rule(400, "SampleRate 必須介於 0 到 1。");
        }
    }

    private static void ValidateSamplingTable(NpgsqlConnection connection, NpgsqlTransaction transaction, int type, IReadOnlyList<Qc103ParameterDto> values)
    {
        if (type != 5) return;
        EnsureTable(connection, transaction, "tblqcsamplingtable");
        var name = RequiredText(values.First(value => string.Equals(value.ParameterNo, "SamplingTable", StringComparison.OrdinalIgnoreCase)).ParameterValue, "SamplingTable", 25);
        using var command = new NpgsqlCommand("SELECT samplingtable FROM \"YMES\".tblqcsamplingtable WHERE lower(samplingtable)=lower(@table) FOR SHARE;", connection, transaction);
        Text(command, "table", name, 25);
        if (command.ExecuteScalar() is null) throw Rule(422, "SamplingTable 不存在，請先維護 QC101 抽樣表格。");
    }

    private static Qc103PlanDto ReadPlan(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, bool locked)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblqcsamplingplan WHERE lower(planno)=lower(@no)" + (locked ? " FOR UPDATE;" : ";"), connection, transaction);
        Text(command, "no", no, 25);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到抽樣計劃，請重新查詢。");
        var result = Read(reader);
        if (reader.Read()) throw Rule(409, "PlanNo 存在大小寫不同的重複資料，請先整理。");
        return result;
    }

    private static IReadOnlyList<Qc103ParameterDto> ReadParameters(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc103PlanDto row)
    {
        using var command = new NpgsqlCommand("SELECT parameterno,parametervalue FROM \"YMES\".tblqcsamplingparameter WHERE lower(planno)=lower(@no) ORDER BY parameterno;", connection, transaction);
        Text(command, "no", row.PlanNo, 25);
        var specs = ParameterSpecs(row.PlanType);
        var result = new List<Qc103ParameterDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var no = reader.GetString(0);
            result.Add(new(no, NullableText(reader, 1), specs.FirstOrDefault(spec => string.Equals(spec.ParameterNo, no, StringComparison.OrdinalIgnoreCase))?.ShowFlag));
        }
        return result;
    }

    private static void ReplaceParameters(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, IReadOnlyList<Qc103ParameterDto> values)
    {
        using (var deletion = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcsamplingparameter WHERE lower(planno)=lower(@no);", connection, transaction)) { Text(deletion, "no", no, 25); deletion.ExecuteNonQuery(); }
        foreach (var value in values)
        {
            using var insert = new NpgsqlCommand("INSERT INTO \"YMES\".tblqcsamplingparameter(planno,parameterno,parametervalue) VALUES(@no,@parameter,@value);", connection, transaction);
            Text(insert, "no", no, 25); Text(insert, "parameter", value.ParameterNo, 20); Text(insert, "value", value.ParameterValue, 255); insert.ExecuteNonQuery();
        }
    }

    private static Qc103PlanDto ReadReturned(NpgsqlCommand command) { using var reader = command.ExecuteReader(); if (!reader.Read()) throw Conflict(); return Read(reader); }
    private static Qc103PlanDto Read(NpgsqlDataReader reader) => new(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), NullableText(reader, 3), NullableText(reader, 4), NullableText(reader, 5), Date(reader, 6), reader.GetInt32(7), reader.GetInt32(8), reader.IsDBNull(9) ? null : reader.GetDecimal(9), NullableText(reader, 10), Date(reader, 11));
    private static void Bind(NpgsqlCommand command, PlanValue value, string actor) { Text(command, "no", value.No, 25); Text(command, "name", value.Name, 50); Number(command, "type", value.Type); Text(command, "function", value.Function, 50); Text(command, "description", value.Description, 255); Number(command, "allow", value.Allow); Text(command, "actor", actor, 10); }
    private static void ExpectedParameters(NpgsqlCommand command, Qc103PlanDto row) { Text(command, "no", row.PlanNo, 25); Number(command, "state", row.IssueState); Number(command, "stamp", row.DataStamp); }
    private static void Expected(Qc103PlanDto row, int? state, decimal? stamp) { if (state is null || state is not (-1 or 0 or 1 or 2)) throw Rule(400, "必須提供有效的 IssueState。"); if (row.IssueState != state || row.DataStamp != stamp) throw Conflict(); }
    private static string Key(string value) => RequiredText(value, "PlanNo", 25).ToUpperInvariant();
    private static string RequiredText(string? value, string field, int size) => OptionalText(value, field, size) ?? throw Rule(400, $"{field} 為必填。");
    private static string? OptionalText(string? value, string field, int size, bool multiline = false) { var text = (value ?? "").Trim(); if (text.Length > size || text.Any(ch => char.IsControl(ch) && !(multiline && (ch is '\r' or '\n' or '\t')))) throw Rule(400, $"{field} 不可超過 {size} 個字元或含非法控制字元。"); return text.Length == 0 ? null : text; }
    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table) { using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@table AND c.relkind IN ('r','p','v','m','f'));", connection, transaction); Text(command, "table", table, 63); return command.ExecuteScalar() is true; }
    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table) { if (!TableExists(connection, transaction, table)) throw Rule(503, $"YMES 尚未建立 {table.ToUpperInvariant()}，請先核對來源 DDL 並套用 migration。"); }
    private static void EnsureTables(NpgsqlConnection connection, NpgsqlTransaction transaction) { EnsureTable(connection, transaction, "tblqcsamplingplan"); EnsureTable(connection, transaction, "tblqcsamplingparameter"); }
    private static void Text(NpgsqlCommand command, string name, string? value, int size) => command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand command, string name, decimal? value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static string? NullableText(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
    private static DateTime? Date(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    private static void Rollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { /* preserve operation error */ } }
    private static Qc103RuleException Rule(int status, string message) => new(status, message);
    private static Qc103RuleException Conflict() => Rule(409, "抽樣計劃已被其他作業修改，請重新查詢後再試。");
}
