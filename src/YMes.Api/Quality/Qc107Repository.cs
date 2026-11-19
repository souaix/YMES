using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc107Repository(NpgsqlDataSource dataSource, IConfiguration configuration)
{
    private const string BasisColumns = "ruleno,componentcollect::int,issuestate::int,creator,createdate,reviser,revisedate";
    private const string DetailColumns = "ruleno,action::int,reasontype::int,reasonlevel::int,rejectnum,rate,ruletype::int,changeaql,creator,createdate,reviser,revisedate";
    private static readonly string[] DraftFields = ["componentCollect", "ruleDetails"];

    private sealed record Basis(string RuleNo, int ComponentCollect, int? IssueState,
        string? Creator, DateTime? CreateDate, string? Reviser, DateTime? ReviseDate);
    private sealed record Snapshot(Basis Basis, IReadOnlyList<Qc107DetailDto> Details);
    private sealed record DetailValue(int Action, int ReasonType, int ReasonLevel, decimal? RejectNum,
        string? Rate, int? RuleType, string? ChangeAql);

    public IReadOnlyList<Qc107BasisDto> List(string? searchFieldName, string? searchFieldValue)
    {
        if (!string.IsNullOrWhiteSpace(searchFieldName) && !string.Equals(searchFieldName.Trim(), "RuleNo", StringComparison.OrdinalIgnoreCase))
            throw Rule(400, "查詢欄位只允許 RuleNo。");
        var search = (searchFieldValue ?? "").Trim();
        if (search.Length > 50 || search.Any(char.IsControl)) throw Rule(400, "RuleNo 查詢最長 50 字元，不可含控制字元。");
        using var connection = dataSource.OpenConnection();
        // A list row and its aggregate version must come from one snapshot.
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        EnsureTables(connection, transaction);
        var rows = new List<Basis>();
        using (var command = new NpgsqlCommand($"SELECT {BasisColumns} FROM \"YMES\".tblqcattrulebasis WHERE @search='' OR ruleno ILIKE @search ESCAPE '' ORDER BY ruleno;", connection, transaction))
        {
            Text(command, "search", search, 50);
            using var reader = command.ExecuteReader();
            while (reader.Read()) rows.Add(ReadBasis(reader));
        }
        var result = rows.Select(row => ToResult(new(row, ReadDetails(connection, transaction, row.RuleNo))).Basis).ToArray();
        transaction.Commit();
        return result;
    }

    public Qc107SaveResult Get(string ruleNo)
    {
        var no = NormalizeKey(ruleNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        EnsureTables(connection, transaction);
        var result = ToResult(ReadSnapshot(connection, transaction, no, false));
        transaction.Commit();
        return result;
    }

    public IReadOnlyList<Qc107Option> ValueList(string name)
    {
        if (name is not ("Actions" or "ReasonTypes")) throw Rule(400, "不支援的 ValueList。");
        // These entries are an adapter for verified source metadata, never a
        // fallback to the legacy comments or Vue DEMO choices.
        var section = configuration.GetSection("QC107:ValueLists:" + name);
        var result = new List<Qc107Option>();
        foreach (var entry in section.GetChildren())
        {
            if (!int.TryParse(entry["Value"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                || value is < -9 or > 9 || string.IsNullOrWhiteSpace(entry["Label"]) || entry["Label"]!.Any(char.IsControl)
                || result.Any(x => x.Value == value))
                throw Rule(503, "QC107 正式 ValueList 設定無效，請依原生 MES 值與標籤修正。");
            result.Add(new(value, entry["Label"]!.Trim()));
        }
        if (result.Count == 0)
            throw Rule(503, name == "Actions"
                ? "尚未交付 TBLQCATTRULEDETAIL.ACTION 的正式 MES ValueList；不以舊註解或 DEMO 選項取代。"
                : "尚未交付 GetValueList(TBLQCATTRULEBASIS,REASONTYPE,True) 的正式值與標籤；不以舊註解或 DEMO 選項取代。");
        return result;
    }

    public Qc107AqlOptions AqlOptions()
    {
        if (!AqlEnabled()) return new(false, []);
        using var connection = dataSource.OpenConnection();
        return ReadAqlOptions(connection, null);
    }

    private bool AqlEnabled() => configuration["QC107:UserDefineAql"]?.Trim() switch
    {
        "0" => false,
        "1" => true,
        _ => throw Rule(503, "尚未核實 SysInfo/UserDefineAQL 的實際設定與參數來源，無法決定自訂 AQL；不預設開啟或關閉。")
    };

    private static Qc107AqlOptions ReadAqlOptions(NpgsqlConnection connection, NpgsqlTransaction? transaction)
    {
        EnsureTable(connection, transaction, "tblqcaql");
        using var command = new NpgsqlCommand("SELECT DISTINCT aql FROM \"YMES\".tblqcaql WHERE aqltype IS NOT NULL ORDER BY aql;", connection, transaction);
        var values = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) values.Add(reader.GetString(0));
        return new(true, values);
    }

    public Qc107SaveResult Create(Qc107WriteRequest request, string actor)
    {
        actor = NormalizeActor(actor);
        var no = NormalizeKey(request.RuleNo);
        var component = Component(request.ComponentCollect);
        var details = NormalizeDetails(request.Details);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTables(connection, transaction);
            using (var duplicate = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblqcattrulebasis WHERE lower(ruleno)=lower(@no);", connection, transaction))
            {
                Text(duplicate, "no", no, 20);
                if (duplicate.ExecuteScalar() is not null) throw Rule(409, "RuleNo 已存在。");
            }
            ValidateChoices(connection, transaction, details, []);
            using (var command = new NpgsqlCommand("INSERT INTO \"YMES\".tblqcattrulebasis(ruleno,componentcollect,creator,createdate,issuestate) VALUES(@no,@component,@actor,localtimestamp,0);", connection, transaction))
            {
                Text(command, "no", no, 20); Number(command, "component", component); Text(command, "actor", actor, 10);
                command.ExecuteNonQuery();
            }
            ReplaceDetails(connection, transaction, no, details, [], actor);
            var result = ToResult(ReadSnapshot(connection, transaction, no, false));
            transaction.Commit();
            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Rollback(transaction); throw Rule(409, "RuleNo 或明細複合鍵已存在。");
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc107SaveResult Update(string ruleNo, int? issueState, string version, Qc107WriteRequest request, string actor)
    {
        actor = NormalizeActor(actor);
        var no = NormalizeKey(ruleNo);
        if (!string.IsNullOrWhiteSpace(request.RuleNo) && !string.Equals(no, NormalizeKey(request.RuleNo), StringComparison.Ordinal))
            throw Rule(400, "RuleNo 不可修改。");
        var component = Component(request.ComponentCollect);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTables(connection, transaction);
            // All writes lock the same parent first, then inspect/replace detail.
            var current = ReadSnapshot(connection, transaction, no, true);
            Expected(current, issueState, version);
            if (current.Basis.IssueState != 0)
                throw Rule(409, "目前只有未簽核資料可修改；其他狀態的共用 Issue Control 尚未提供可編輯欄位。");
            // Match the raw DTO against the locked snapshot before applying
            // new-write normalization, so legacy nullable values stay nullable.
            var details = NormalizeDetails(request.Details, current.Details);
            ValidateChoices(connection, transaction, details, current.Details);
            using (var command = new NpgsqlCommand("UPDATE \"YMES\".tblqcattrulebasis SET componentcollect=@component,reviser=@actor,revisedate=localtimestamp WHERE ruleno=@no;", connection, transaction))
            {
                Text(command, "no", current.Basis.RuleNo, 20); Number(command, "component", component); Text(command, "actor", actor, 10);
                if (command.ExecuteNonQuery() != 1) throw Conflict();
            }
            if (!SameDetails(details, current.Details)) ReplaceDetails(connection, transaction, current.Basis.RuleNo, details, current.Details, actor);
            var result = ToResult(ReadSnapshot(connection, transaction, current.Basis.RuleNo, false));
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc107SaveResult Delete(string ruleNo, int? issueState, string version, string actor)
    {
        NormalizeActor(actor);
        var no = NormalizeKey(ruleNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTables(connection, transaction);
            var current = ReadSnapshot(connection, transaction, no, true);
            Expected(current, issueState, version);
            if (current.Basis.IssueState is not (0 or 2 or -1)) throw Rule(409, "只有狀態 0、2、-1 的法則可刪除；簽核中或 NULL 狀態不可刪除。");
            EnsureNoReferences(connection, transaction, current.Basis.RuleNo);
            DeleteDetails(connection, transaction, current.Basis.RuleNo);
            using var command = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcattrulebasis WHERE ruleno=@no;", connection, transaction);
            Text(command, "no", current.Basis.RuleNo, 20);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
            transaction.Commit();
            return ToResult(current);
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc107SaveResult Approve(string ruleNo, int? issueState, string version, string actor)
    {
        actor = NormalizeActor(actor);
        var no = NormalizeKey(ruleNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTables(connection, transaction);
            var current = ReadSnapshot(connection, transaction, no, true);
            Expected(current, issueState, version);
            if (current.Basis.IssueState != 0) throw Rule(409, "只有未簽核資料可核准。");
            if (current.Details.Count == 0) throw Rule(422, "此 RuleNo 沒有明細，無法核准。");
            if (new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" }.Any(table => TableExists(connection, transaction, table)))
                throw Rule(503, "YMES 已存在 Issue/Job 流程表，QC107 尚未接入 QCITEM_A RULE DATA 共用簽核；資料未變更。");
            using (var command = new NpgsqlCommand("UPDATE \"YMES\".tblqcattrulebasis SET issuestate=2,reviser=@actor,revisedate=localtimestamp WHERE ruleno=@no;", connection, transaction))
            {
                Text(command, "no", current.Basis.RuleNo, 20); Text(command, "actor", actor, 10);
                if (command.ExecuteNonQuery() != 1) throw Conflict();
            }
            var result = ToResult(ReadSnapshot(connection, transaction, current.Basis.RuleNo, false));
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    private static List<DetailValue> NormalizeDetails(List<Qc107DetailRequest>? requests, IReadOnlyList<Qc107DetailDto>? previous = null)
    {
        if (requests is null) throw Rule(400, "必須明確提供 details 陣列；空明細請傳 []。");
        if (requests.Count > 1000) throw Rule(400, "單一法則最多 1000 筆明細。");
        var result = new List<DetailValue>();
        var keys = new HashSet<(int, int, int)>();
        foreach (var row in requests)
        {
            if (row is null) throw Rule(400, "明細不可為 NULL。");
            if (row.Action is null or < -9 or > 9 || row.ReasonType is null or < -9 or > 9 || row.ReasonLevel is null or < -1 or > 9)
                throw Rule(400, "Action、ReasonType 必須為來源 numeric(1,0) 的整數，ReasonLevel 必須為 -1 到 9。");
            if (!keys.Add((row.Action.Value, row.ReasonType.Value, row.ReasonLevel.Value))) throw Rule(400, "明細 Action／ReasonType／ReasonLevel 複合鍵不可重複。");
            var original = previous?.FirstOrDefault(old => SameRawDetail(row, old));
            if (original is not null)
            {
                // A valid snapshot/version authorizes preservation of the exact
                // stored row, not reinterpretation of NULL as 0/default modes.
                result.Add(new(original.Action, original.ReasonType, original.ReasonLevel, original.RejectNum,
                    original.Rate, original.RuleType, original.ChangeAql));
                continue;
            }
            if (row.RuleType is not (0 or 1 or 2)) throw Rule(400, "RuleType 只允許 0 Number、1 Rate、2 AQL Reject Number；來源空值只能原樣保留，修改時須明確選擇。");
            decimal reject = 0;
            string? rate = null;
            string? aql = null;
            if (row.RuleType == 0)
            {
                if (!row.RejectNum.HasValue || row.RejectNum.Value < 1 || row.RejectNum.Value > 9999999999m || decimal.Truncate(row.RejectNum.Value) != row.RejectNum.Value)
                    throw Rule(400, "Number 的 RejectNum 必填，且須為 1 到 9999999999 的整數。");
                reject = row.RejectNum.Value;
            }
            else if (row.RuleType == 1)
            {
                rate = (row.Rate ?? "").Trim();
                if (rate.Length is < 1 or > 10 || !double.TryParse(rate, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) || !double.IsFinite(parsed) || parsed <= 0)
                    throw Rule(400, "Rate 必須為大於 0 的數字，最長 10 字元。");
            }
            else
            {
                aql = (row.ChangeAql ?? "").Trim();
                if (aql.Length > 30 || aql.Any(char.IsControl)) throw Rule(400, "ChangeAQL 最長 30 字元，不可含控制字元。");
                if (aql.Length == 0) aql = null;
            }
            result.Add(new(row.Action.Value, row.ReasonType.Value, row.ReasonLevel.Value, reject, rate, row.RuleType.Value, aql));
        }
        return result.OrderBy(x => x.Action).ThenBy(x => x.ReasonType).ThenBy(x => x.ReasonLevel).ToList();
    }

    private static bool SameRawDetail(Qc107DetailRequest row, Qc107DetailDto old) => row.Action == old.Action && row.ReasonType == old.ReasonType
        && row.ReasonLevel == old.ReasonLevel && row.RejectNum == old.RejectNum && row.Rate == old.Rate && row.RuleType == old.RuleType && row.ChangeAql == old.ChangeAql;

    private void ValidateChoices(NpgsqlConnection connection, NpgsqlTransaction transaction, IReadOnlyList<DetailValue> details, IReadOnlyList<Qc107DetailDto> current)
    {
        var changed = details.Where(row => !current.Any(old => SameDetail(row, old))).ToArray();
        if (changed.Length == 0) return;
        var actions = ValueList("Actions");
        var reasons = ValueList("ReasonTypes");
        foreach (var row in changed)
        {
            if (!actions.Any(x => x.Value == row.Action)) throw Rule(422, "Action 不在正式 MES ValueList，請重新選取。");
            if (!reasons.Any(x => x.Value == row.ReasonType)) throw Rule(422, "ReasonType 不在正式 MES ValueList，請重新選取。");
        }
        var aqlRows = changed.Where(x => x.RuleType == 2).ToArray();
        if (aqlRows.Length == 0) return;
        if (!AqlEnabled()) throw Rule(422, "SysInfo/UserDefineAQL 尚未啟用，不可新增或修改 AQL 規則。");
        if (aqlRows.All(x => x.ChangeAql is null)) return;
        var options = ReadAqlOptions(connection, transaction);
        if (aqlRows.Any(x => x.ChangeAql is not null && !options.Items.Contains(x.ChangeAql, StringComparer.Ordinal)))
            throw Rule(422, "ChangeAQL 不在已核實的共用 AQL 選項，請重新選取；可留空白。");
    }

    private static bool SameDetail(DetailValue row, Qc107DetailDto old) => row.Action == old.Action && row.ReasonType == old.ReasonType
        && row.ReasonLevel == old.ReasonLevel && row.RejectNum == old.RejectNum && row.Rate == old.Rate && row.RuleType == old.RuleType && row.ChangeAql == old.ChangeAql;
    private static bool SameDetails(IReadOnlyList<DetailValue> rows, IReadOnlyList<Qc107DetailDto> current) =>
        rows.Count == current.Count && rows.All(row => current.Any(old => SameDetail(row, old)));

    private static void ReplaceDetails(NpgsqlConnection connection, NpgsqlTransaction transaction, string no,
        IReadOnlyList<DetailValue> rows, IReadOnlyList<Qc107DetailDto> previous, string actor)
    {
        DeleteDetails(connection, transaction, no);
        using var clock = new NpgsqlCommand("SELECT localtimestamp;", connection, transaction);
        var now = (DateTime)clock.ExecuteScalar()!;
        foreach (var row in rows)
        {
            var old = previous.FirstOrDefault(x => x.Action == row.Action && x.ReasonType == row.ReasonType && x.ReasonLevel == row.ReasonLevel);
            var unchanged = old is not null && SameDetail(row, old);
            using var command = new NpgsqlCommand("""
                INSERT INTO "YMES".tblqcattruledetail(ruleno,action,reasontype,reasonlevel,rejectnum,rate,ruletype,changeaql,creator,createdate,reviser,revisedate)
                VALUES(@no,@action,@reason,@level,@reject,@rate,@type,@aql,@creator,@created,@reviser,@revised);
                """, connection, transaction);
            Text(command, "no", no, 20); Number(command, "action", row.Action); Number(command, "reason", row.ReasonType); Number(command, "level", row.ReasonLevel);
            Number(command, "reject", row.RejectNum); Text(command, "rate", row.Rate, 10); Number(command, "type", row.RuleType); Text(command, "aql", row.ChangeAql, 30);
            Text(command, "creator", old is null ? actor : old.Creator, 10); Timestamp(command, "created", old is null ? now : old.CreateDate);
            Text(command, "reviser", old is null ? null : unchanged ? old.Reviser : actor, 10);
            Timestamp(command, "revised", old is null ? null : unchanged ? old.ReviseDate : now);
            command.ExecuteNonQuery();
        }
    }

    private static void DeleteDetails(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcattruledetail WHERE lower(ruleno)=lower(@no);", connection, transaction);
        Text(command, "no", no, 20); command.ExecuteNonQuery();
    }

    private static Snapshot ReadSnapshot(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, bool forUpdate)
    {
        Basis basis;
        using (var command = new NpgsqlCommand($"SELECT {BasisColumns} FROM \"YMES\".tblqcattrulebasis WHERE lower(ruleno)=lower(@no){(forUpdate ? " FOR UPDATE" : "")};", connection, transaction))
        {
            Text(command, "no", no, 20);
            using var reader = command.ExecuteReader();
            if (!reader.Read()) throw Rule(404, "找不到指定的計數判別法則。");
            basis = ReadBasis(reader);
            if (reader.Read()) throw Rule(409, "RuleNo 存在大小寫不同的重複資料，請先整理。");
        }
        return new(basis, ReadDetails(connection, transaction, basis.RuleNo));
    }

    private static IReadOnlyList<Qc107DetailDto> ReadDetails(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand($"SELECT {DetailColumns} FROM \"YMES\".tblqcattruledetail WHERE lower(ruleno)=lower(@no) ORDER BY action,reasontype,reasonlevel;", connection, transaction);
        Text(command, "no", no, 20);
        var result = new List<Qc107DetailDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(new(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3),
            reader.IsDBNull(4) ? null : reader.GetDecimal(4), String(reader, 5), Int(reader, 6), String(reader, 7), String(reader, 8), Date(reader, 9), String(reader, 10), Date(reader, 11)));
        return result;
    }

    private static Basis ReadBasis(NpgsqlDataReader r) => new(r.GetString(0), r.GetInt32(1), Int(r, 2), String(r, 3), Date(r, 4), String(r, 5), Date(r, 6));

    private static Qc107SaveResult ToResult(Snapshot snapshot)
    {
        var row = snapshot.Basis;
        var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot))));
        return new(new(row.RuleNo, row.ComponentCollect, row.IssueState, row.Creator, row.CreateDate, row.Reviser, row.ReviseDate,
            row.IssueState == 0 ? DraftFields : [], version), snapshot.Details);
    }

    private static void Expected(Snapshot snapshot, int? issueState, string version)
    {
        if (snapshot.Basis.IssueState != issueState || !string.Equals(ToResult(snapshot).Basis.Version, version, StringComparison.Ordinal)) throw Conflict();
    }

    private static void EnsureNoReferences(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        // Verified QC201/QC203 columns only. Their writers must lock this basis
        // FOR SHARE before assigning it to coordinate with our FOR UPDATE lock.
        if (TableExists(connection, transaction, "tblqcitemforattributes"))
        {
            using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblqcitemforattributes WHERE lower(ruleno)=lower(@no) LIMIT 1;", connection, transaction);
            Text(command, "no", no, 20);
            if (command.ExecuteScalar() is not null) throw Rule(409, "此法則已被計數品管項目引用，不可刪除。");
        }
        if (TableExists(connection, transaction, "tblspcqattrulemapdetail"))
        {
            using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblspcqattrulemapdetail WHERE lower(lotruleno)=lower(@no) OR lower(comruleno)=lower(@no) LIMIT 1;", connection, transaction);
            Text(command, "no", no, 20);
            if (command.ExecuteScalar() is not null) throw Rule(409, "此法則已被計數品管項目判別法則對應引用，不可刪除。");
        }
    }

    private static string NormalizeKey(string? value)
    {
        var no = (value ?? "").Trim().ToUpperInvariant();
        if (no.Length is < 1 or > 20 || no.Any(char.IsControl)) throw Rule(400, "RuleNo 為必填、最多 20 字元，不可含控制字元。");
        return no;
    }
    private static int Component(int? value) => value is 0 or 1 ? value.Value : throw Rule(400, "ComponentCollect 只允許 0 或 1。");
    private static string NormalizeActor(string actor)
    {
        var value = actor.Trim();
        if (value.Length is < 1 or > 10 || value.Any(char.IsControl)) throw Rule(401, "登入帳號不可空白，最長 10 字元。");
        return value;
    }
    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string name)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@name AND c.relkind IN ('r','p','v','m','f'));", connection, transaction);
        Text(command, "name", name, 63); return command.ExecuteScalar() is true;
    }
    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction, string name)
    {
        if (!TableExists(connection, transaction, name)) throw Rule(503, $"YMES 尚未建立 {name}，請先比對來源 DDL 與 migration。");
    }
    private static void EnsureTables(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        EnsureTable(connection, transaction, "tblqcattrulebasis"); EnsureTable(connection, transaction, "tblqcattruledetail");
    }
    private static void Text(NpgsqlCommand command, string name, string? value, int size) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand command, string name, decimal? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static void Timestamp(NpgsqlCommand command, string name, DateTime? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Timestamp).Value = (object?)value ?? DBNull.Value;
    private static string? String(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
    private static int? Int(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetInt32(index);
    private static DateTime? Date(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    private static void Rollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { /* Preserve original error. */ } }
    private static Qc107RuleException Rule(int status, string message) => new(status, message);
    private static Qc107RuleException Conflict() => Rule(409, "法則主檔或明細已被其他作業修改，請重新查詢後再試。");
}
