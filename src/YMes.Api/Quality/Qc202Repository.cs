using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc202Repository(NpgsqlDataSource dataSource)
{
    private const decimal MaxStamp = 99999999999m;
    private const string SelectColumns = "qcitemno,factorvalues::text,us,ls,ucl,lcl,cl,ruleset,creator,createdate,reviser,revisedate,datastamp";

    public IReadOnlyList<Qc202ControlLimitDto> List(string qcItemNo)
    {
        var no = ValidateCode(qcItemNo, "QCItemNo", 25, true);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        no = ReadApprovedItem(connection, transaction, no);
        using var command = new NpgsqlCommand($"SELECT {SelectColumns} FROM \"YMES\".tblspcattlimit WHERE lower(btrim(qcitemno))=lower(btrim(@no)) ORDER BY factorvalues::text,qcitemno;", connection, transaction);
        AddVarchar(command, "no", no);
        var rows = new List<Qc202ControlLimitDto>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) rows.Add(Map(reader) with { QcItemNo = no });
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var keys = ValidateDistinctKeys(connection, transaction, row.FactorValues.Keys, 409);
            var identity = JsonSerializer.Serialize(row.FactorValues.ToDictionary(x => keys[x.Key], x => x.Value, StringComparer.Ordinal)
                .OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal));
            if (!identities.Add(identity)) throw Rule(409, "同一 QC Item／因子組合存在大小寫或前後空白歧義來源列，請先整理資料。");
        }
        transaction.Commit();
        return rows;
    }

    public Qc202OptionsDto Options(string qcItemNo)
    {
        var no = ValidateCode(qcItemNo, "QCItemNo", 25, true);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        no = ReadApprovedItem(connection, transaction, no);
        var missing = new List<string>();
        var definitions = GetFactorDefinitions(connection, transaction, no, missing);
        var factors = definitions.Select(f => new Qc202FactorOptionDto(f.No, f.Name, f.Type,
            f.FreeTextAvailable ? Array.Empty<string>() : null, f.MetadataAvailable, f.FreeTextAvailable)).ToArray();
        var rules = ReadRuleOptions(connection, transaction, missing);
        var factorOptions = factors.Where(f => f.ReferenceValuesAvailable)
            .ToDictionary(f => f.FactorNo, f => f.Options!, StringComparer.Ordinal);
        transaction.Commit();
        return new Qc202OptionsDto(factors, rules, factorOptions, missing.Distinct(StringComparer.Ordinal).ToArray());
    }

    public Qc202ControlLimitDto Create(Qc202ControlLimitWriteRequest request, string actor)
    {
        actor = ValidateActor(actor);
        var no = ValidateCode(request.QcItemNo, "QCItemNo", 25, true);
        var limits = ValidateLimits(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            AcquireItem(connection, transaction, no);
            no = ReadApprovedItem(connection, transaction, no);
            var factors = NormalizeFactorValues(connection, transaction, no, request.FactorValues);
            var ruleSet = ValidateRuleSet(connection, transaction, request.RuleSet);
            EnsureNoDuplicate(connection, transaction, no, factors);
            using var command = new NpgsqlCommand($"""
                INSERT INTO "YMES".tblspcattlimit
                    (qcitemno,factorvalues,us,ls,ucl,lcl,cl,ruleset,creator,createdate,datastamp)
                VALUES(@no,@factors,@us,@ls,@ucl,@lcl,@cl,@rules,@actor,now() AT TIME ZONE 'UTC',0)
                RETURNING {SelectColumns};
                """, connection, transaction);
            BindLimit(command, no, factors, limits, ruleSet, actor);
            var result = Returned(command);
            transaction.Commit();
            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            TryRollback(transaction);
            throw Rule(409, "相同 QCItemNo 與線上因子組合已存在。");
        }
        catch { TryRollback(transaction); throw; }
    }

    public Qc202ControlLimitDto Update(string qcItemNo, Qc202UpdateRequest request, string actor)
    {
        actor = ValidateActor(actor);
        var no = ValidateCode(qcItemNo, "QCItemNo", 25, true);
        ValidateStamp(request.DataStamp);
        var factors = ValidateExistingFactorValues(request.OriginalFactorValues);
        var limits = ValidateLimits(request);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            AcquireItem(connection, transaction, no);
            no = ReadApprovedItem(connection, transaction, no);
            ValidateBodyItem(connection, transaction, no, request.QcItemNo);
            _ = ReadFactorRows(connection, transaction, no, requireAny: false);
            ValidateDistinctKeys(connection, transaction, factors.Keys, 400);
            var ruleSet = ValidateRuleSet(connection, transaction, request.RuleSet);
            var current = ReadLimitForUpdate(connection, transaction, no, factors);
            EnsureStamp(current.DataStamp, request.DataStamp, increment: true);
            using var command = new NpgsqlCommand($"""
                UPDATE "YMES".tblspcattlimit
                SET us=@us,ls=@ls,ucl=@ucl,lcl=@lcl,cl=@cl,ruleset=@rules,
                    reviser=@actor,revisedate=now() AT TIME ZONE 'UTC',datastamp=COALESCE(datastamp,0)+1
                WHERE qcitemno=@no AND factorvalues=@factors AND datastamp IS NOT DISTINCT FROM @stamp
                RETURNING {SelectColumns};
                """, connection, transaction);
            BindLimit(command, current.QcItemNo, factors, limits, ruleSet, actor);
            AddNumeric(command, "stamp", request.DataStamp);
            var result = Returned(command) with { QcItemNo = no };
            transaction.Commit();
            return result;
        }
        catch { TryRollback(transaction); throw; }
    }

    public void Delete(string qcItemNo, Qc202DeleteRequest request)
    {
        var no = ValidateCode(qcItemNo, "QCItemNo", 25, true);
        ValidateStamp(request.DataStamp);
        var factors = ValidateExistingFactorValues(request.FactorValues);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            AcquireItem(connection, transaction, no);
            no = ReadApprovedItem(connection, transaction, no);
            _ = ReadFactorRows(connection, transaction, no, requireAny: false);
            ValidateDistinctKeys(connection, transaction, factors.Keys, 400);
            var current = ReadLimitForUpdate(connection, transaction, no, factors);
            EnsureStamp(current.DataStamp, request.DataStamp, increment: false);
            using var command = new NpgsqlCommand("DELETE FROM \"YMES\".tblspcattlimit WHERE qcitemno=@no AND factorvalues=@factors AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction);
            AddVarchar(command, "no", current.QcItemNo);
            AddJson(command, "factors", factors);
            AddNumeric(command, "stamp", request.DataStamp);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
            transaction.Commit();
        }
        catch { TryRollback(transaction); throw; }
    }

    private static void AcquireItem(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        try { QcItemIdentityGuard.Acquire(connection, transaction, [no]); }
        catch (Qc201RuleException ex) { throw Rule(ex.StatusCode, ex.Message); }
    }

    private static string ReadApprovedItem(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        try { QcItemIdentityGuard.EnsureAttributeOwner(connection, transaction, no); }
        catch (Qc201RuleException ex) { throw Rule(ex.StatusCode, ex.Message); }
        RequireTable(connection, transaction, "tblspcattlimit", "QC202 管制內容");
        using var command = new NpgsqlCommand("""
            SELECT qcitemno,issuestate::int FROM "YMES".tblqcitemforattributes
            WHERE lower(btrim(qcitemno))=lower(btrim(@no))
            ORDER BY lower(btrim(qcitemno)) COLLATE "C",qcitemno COLLATE "C" FOR SHARE;
            """, connection, transaction);
        AddVarchar(command, "no", no);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到指定的計數品管項目。");
        var canonical = reader.GetString(0);
        var approved = !reader.IsDBNull(1) && reader.GetInt32(1) == 2;
        if (reader.Read()) throw Rule(409, "QC Item 存在大小寫／前後空白不同的重複主檔，無法安全決定管制內容歸屬。");
        if (!approved) throw Rule(409, "計數管制內容只允許套用到已核准的 QC Item（IssueState 必須為 2）。");
        return canonical;
    }

    private sealed record FactorDefinition(string No, string Key, string? Name = null, int? Type = null,
        int? State = null, bool MetadataAvailable = false)
    {
        public bool FreeTextAvailable => MetadataAvailable && State == 2 && Type == 2;
    }

    private static List<FactorDefinition> ReadFactorRows(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string no, bool requireAny)
    {
        RequireTable(connection, transaction, "tblqcitemfactor", "QC201 要因");
        using var command = new NpgsqlCommand("""
            SELECT factorno,lower(btrim(factorno)) FROM "YMES".tblqcitemfactor
            WHERE lower(btrim(qcitemno))=lower(btrim(@no)) AND upper(factoronline)='Y'
            ORDER BY lower(btrim(factorno)) COLLATE "C",factorno COLLATE "C",qcitemno COLLATE "C" FOR SHARE;
            """, connection, transaction);
        AddVarchar(command, "no", no);
        using var reader = command.ExecuteReader();
        var rows = new List<FactorDefinition>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
        {
            var number = reader.GetString(0);
            var key = reader.GetString(1);
            if (number.Length is < 1 or > 30 || key.Length == 0 || number.Any(char.IsControl))
                throw Rule(409, "線上 FactorNo 含無效來源識別碼，請先整理來源定義。");
            if (!keys.Add(key)) throw Rule(409, "線上 FactorNo 存在大小寫／前後空白歧義，請先整理來源定義。");
            rows.Add(new FactorDefinition(number, key));
        }
        if (requireAny && rows.Count == 0) throw Rule(422, "此 QC Item 沒有 FactorOnline=Y 的要因，無法設定管制內容。");
        return rows;
    }

    private static List<FactorDefinition> GetFactorDefinitions(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string no, List<string> missing)
    {
        var factors = ReadFactorRows(connection, transaction, no, requireAny: true);
        if (!TableExists(connection, transaction, "tblsysmesparameter"))
        {
            missing.Add("TBLSYSMESPARAMETER：尚無正式要因名稱、ParameterType 與參照來源 metadata。");
            return factors;
        }
        var metadata = new Dictionary<string, (string? Name, int? Type, int? State)>(StringComparer.Ordinal);
        using (var command = new NpgsqlCommand("""
            SELECT lower(btrim(parameterno)),parametername,parametertype::int,issuestate::int
            FROM "YMES".tblsysmesparameter WHERE lower(btrim(parameterno))=ANY(@keys)
            ORDER BY lower(btrim(parameterno)) COLLATE "C",parameterno COLLATE "C" FOR SHARE;
            """, connection, transaction))
        {
            command.Parameters.Add("keys", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = factors.Select(f => f.Key).ToArray();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (!metadata.TryAdd(reader.GetString(0), (Text(reader, 1), Integer(reader, 2), Integer(reader, 3))))
                    throw Rule(409, "要因目錄存在大小寫／前後空白歧義，無法安全決定 ParameterType。");
            }
        }
        return factors.Select(f =>
        {
            if (!metadata.TryGetValue(f.Key, out var value))
            {
                missing.Add($"Factor {f.No}：缺少正式要因 metadata。");
                return f;
            }
            var updated = f with { Name = value.Name, Type = value.Type, State = value.State,
                MetadataAvailable = value.Name is not null && value.Type is not null };
            if (!updated.MetadataAvailable) missing.Add($"Factor {f.No}：要因 metadata 不完整。");
            else if (updated.State != 2) missing.Add($"Factor {f.No}：要因目錄尚未核准，不提供新的自由輸入值。");
            else if (updated.Type != 2) missing.Add($"Factor {f.No}：正式 MES Parameter Reference provider 尚未接入；僅可使用全部值 (*)。");
            return updated;
        }).ToList();
    }

    private static Dictionary<string, string> NormalizeFactorValues(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string no, Dictionary<string, string>? submitted)
    {
        if (submitted is null) throw Rule(400, "factorValues 必須提供 JSON 物件。");
        var definitions = GetFactorDefinitions(connection, transaction, no, []);
        var submittedKeys = ValidateDistinctKeys(connection, transaction, submitted.Keys, 400);
        var byKey = definitions.ToDictionary(f => f.Key, StringComparer.Ordinal);
        var incoming = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in submitted)
        {
            if (!byKey.ContainsKey(submittedKeys[key])) throw Rule(400, $"FactorNo {key} 不屬於此 QC Item 的 FactorOnline 清單。");
            incoming.Add(submittedKeys[key], value);
        }
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var factor in definitions)
        {
            incoming.TryGetValue(factor.Key, out var raw);
            var value = (raw ?? "").Trim();
            if (value.Length == 0) value = "*";
            if (value.Length > 61 || value.Any(char.IsControl)) throw Rule(400, $"因子 {factor.No} 的值不可超過 61 個字元或含控制字元。");
            if (value != "*")
            {
                if (!factor.MetadataAvailable) throw Rule(503, $"因子 {factor.No} 缺少正式 metadata，無法判定輸入值規則。");
                if (factor.State != 2) throw Rule(422, $"因子 {factor.No} 的正式要因目錄尚未核准。");
                if (factor.Type != 2) throw Rule(503, $"因子 {factor.No} 的正式 MES Parameter Reference provider 尚未接入。");
                value = value.ToUpperInvariant();
                if (value.Length > 61) throw Rule(400, $"因子 {factor.No} 轉大寫後超過 61 個字元。");
            }
            result.Add(factor.No, value);
        }
        return result;
    }

    // Existing identities are opaque JSONB values. Never re-normalize their
    // case, whitespace, missing legacy keys or values during edit/delete.
    private static Dictionary<string, string> ValidateExistingFactorValues(Dictionary<string, string>? submitted)
    {
        if (submitted is null || submitted.Count == 0) throw Rule(400, "originalFactorValues/factorValues 不可為空。");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in submitted)
        {
            if (key.Length is < 1 or > 30 || key.Trim(' ').Length == 0 || key.Any(char.IsControl))
                throw Rule(400, "factorValues 含無效因子鍵。");
            if (value is null || value.Length > 61 || value.Any(char.IsControl))
                throw Rule(400, $"因子 {key} 的原始值超出資料庫欄位範圍。");
            result.Add(key, value);
        }
        return result;
    }

    private static Dictionary<string, string> ValidateDistinctKeys(NpgsqlConnection connection, NpgsqlTransaction transaction,
        IEnumerable<string> submitted, int errorStatus)
    {
        var names = submitted.ToArray();
        foreach (var name in names)
            if (name.Length is < 1 or > 30 || name.Trim(' ').Length == 0 || name.Any(char.IsControl))
                throw Rule(errorStatus, "factorValues 含無效因子鍵。");
        using var command = new NpgsqlCommand("SELECT value,lower(btrim(value)) FROM unnest(@names) AS input(value);", connection, transaction);
        command.Parameters.Add("names", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = names;
        using var reader = command.ExecuteReader();
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
        {
            var key = reader.GetString(1);
            if (!keys.Add(key)) throw Rule(errorStatus, "factorValues 不可包含大小寫／前後空白重複的因子鍵。");
            result.Add(reader.GetString(0), key);
        }
        return result;
    }

    private static void EnsureNoDuplicate(NpgsqlConnection connection, NpgsqlTransaction transaction, string no,
        Dictionary<string, string> factors)
    {
        var keys = ValidateDistinctKeys(connection, transaction, factors.Keys, 400);
        var canonical = factors.ToDictionary(x => keys[x.Key], x => x.Value, StringComparer.Ordinal);
        var existing = new List<Dictionary<string, string>>();
        using (var command = new NpgsqlCommand("SELECT factorvalues::text FROM \"YMES\".tblspcattlimit WHERE lower(btrim(qcitemno))=lower(btrim(@no)) ORDER BY factorvalues::text,qcitemno FOR UPDATE;", connection, transaction))
        {
            AddVarchar(command, "no", no);
            using var reader = command.ExecuteReader();
            while (reader.Read()) existing.Add(ReadStoredFactors(reader.GetString(0)));
        }
        foreach (var row in existing)
        {
            var oldKeys = ValidateDistinctKeys(connection, transaction, row.Keys, 409);
            var normalized = row.ToDictionary(x => oldKeys[x.Key], x => x.Value, StringComparer.Ordinal);
            if (normalized.Count == canonical.Count && normalized.All(x => canonical.TryGetValue(x.Key, out var value) && value == x.Value))
                throw Rule(409, "相同 QCItemNo 與線上因子組合已存在。");
        }
    }

    private static IReadOnlyList<Qc202RuleOptionDto> ReadRuleOptions(NpgsqlConnection connection, NpgsqlTransaction transaction,
        List<string> missing)
    {
        if (!TableExists(connection, transaction, "tblqcrulebasis"))
        {
            missing.Add("TBLQCRULEBASIS：QC108 計量品管法則尚未建立。");
            return [];
        }
        using var command = new NpgsqlCommand("SELECT ruleno,rulename,issuestate::int,lower(btrim(ruleno)) FROM \"YMES\".tblqcrulebasis ORDER BY ruleserial;", connection, transaction);
        using var reader = command.ExecuteReader();
        var rows = new List<(string No, string? Name, int? State, string Key)>();
        while (reader.Read()) rows.Add((reader.GetString(0), Text(reader, 1), Integer(reader, 2), reader.GetString(3)));
        foreach (var group in rows.GroupBy(r => r.Key, StringComparer.Ordinal))
            if (group.Count() > 1 && group.Any(r => r.State == 2)) throw Rule(409, "QC108 RuleNo 存在大小寫／前後空白歧義，請先整理法則來源。");
        return rows.Where(r => r.State == 2).Select(r => new Qc202RuleOptionDto(r.No, r.Name)).ToArray();
    }

    private static string? ValidateRuleSet(NpgsqlConnection connection, NpgsqlTransaction transaction, string? submitted)
    {
        var text = submitted?.Trim() ?? "";
        if (text.Length > 500 || text.Any(char.IsControl)) throw Rule(400, "RuleSet 不可超過 500 個字元或含控制字元。");
        if (text.Length == 0) return null;
        RequireTable(connection, transaction, "tblqcrulebasis", "QC108 計量品管法則");
        var rules = new List<(string No, string Action)>();
        foreach (var entry in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = entry.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || parts[0].Length is < 1 or > 30 || parts[1] is not ("Warning" or "Hold" or "Alert"))
                throw Rule(400, "RuleSet 格式需為 RuleNo,Warning|Hold|Alert，以分號分隔。");
            rules.Add((parts[0], parts[1]));
        }
        if (rules.Count == 0) return null;
        var requested = ValidateDistinctKeys(connection, transaction, rules.Select(r => r.No), 400);
        var found = new Dictionary<string, (string No, int? State)>(StringComparer.Ordinal);
        using (var command = new NpgsqlCommand("""
            SELECT ruleno,issuestate::int,lower(btrim(ruleno)) FROM "YMES".tblqcrulebasis
            WHERE lower(btrim(ruleno))=ANY(@keys) ORDER BY ruleserial FOR SHARE;
            """, connection, transaction))
        {
            command.Parameters.Add("keys", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = requested.Values.ToArray();
            using var reader = command.ExecuteReader();
            while (reader.Read())
                if (!found.TryAdd(reader.GetString(2), (reader.GetString(0), Integer(reader, 1))))
                    throw Rule(409, "RuleNo 存在大小寫／前後空白歧義，未儲存管制內容。");
        }
        var canonical = new List<string>();
        foreach (var rule in rules)
        {
            if (!found.TryGetValue(requested[rule.No], out var actual) || actual.State != 2)
                throw Rule(422, $"計量法則 {rule.No} 不存在或尚未核准。");
            if (actual.No.IndexOfAny([',', ';']) >= 0)
                throw Rule(422, "正式 RuleNo 含 RuleSet 分隔符號，無法安全寫入來源格式。");
            canonical.Add($"{actual.No},{rule.Action}");
        }
        var result = string.Join(';', canonical);
        if (result.Length > 500) throw Rule(400, "正式 RuleNo 組成的 RuleSet 超過 500 個字元。");
        return result;
    }

    private sealed record Limits(string? Us, string? Ls, string? Ucl, string? Lcl, string? Cl);

    private static Limits ValidateLimits(Qc202LimitFieldsRequest request)
    {
        var values = new[]
        {
            (Name: "US", Text: ValidateLimit(request.Us, "US", false)),
            (Name: "UCL", Text: ValidateLimit(request.Ucl, "UCL", true)),
            (Name: "CL", Text: ValidateLimit(request.Cl, "CL", false)),
            (Name: "LCL", Text: ValidateLimit(request.Lcl, "LCL", false)),
            (Name: "LS", Text: ValidateLimit(request.Ls, "LS", false))
        };
        var numbers = values.Where(v => v.Text is not null).Select(v => decimal.Parse(v.Text!, NumberStyles.Float, CultureInfo.InvariantCulture)).ToArray();
        for (var i = 1; i < numbers.Length; i++)
            if (numbers[i - 1] < numbers[i]) throw Rule(400, "界限順序需符合 US ≥ UCL ≥ CL ≥ LCL ≥ LS。");
        if (values[0].Text is { } us && values[4].Text is { } ls &&
            decimal.Parse(us, NumberStyles.Float, CultureInfo.InvariantCulture) <= decimal.Parse(ls, NumberStyles.Float, CultureInfo.InvariantCulture))
            throw Rule(400, "US 必須大於 LS。");
        return new(values[0].Text, values[4].Text, values[1].Text, values[3].Text, values[2].Text);
    }

    private static string? ValidateLimit(string? value, string field, bool required)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0 && !required) return null;
        if (text.Length == 0 || text.Length > 15 || text.Contains(',') || text.Any(char.IsControl) ||
            !decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            throw Rule(400, $"{field} 必須為 15 字元內的數字。");
        return text;
    }

    private static void ValidateStamp(decimal? stamp)
    {
        if (stamp is { } value && (value != decimal.Truncate(value) || value < -MaxStamp || value > MaxStamp))
            throw Rule(400, "DataStamp 必須為來源 numeric(11,0) 範圍內的整數或明確 NULL。");
    }

    private static void EnsureStamp(decimal? actual, decimal? expected, bool increment)
    {
        if (actual != expected) throw Conflict();
        if (increment && actual == MaxStamp) throw Rule(409, "DataStamp 已達來源欄位上限，無法遞增；資料未變更。");
    }

    private static Qc202ControlLimitDto ReadLimitForUpdate(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string no, Dictionary<string, string> factors)
    {
        using var command = new NpgsqlCommand($"SELECT {SelectColumns} FROM \"YMES\".tblspcattlimit WHERE lower(btrim(qcitemno))=lower(btrim(@no)) AND factorvalues=@factors ORDER BY qcitemno FOR UPDATE;", connection, transaction);
        AddVarchar(command, "no", no);
        AddJson(command, "factors", factors);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, "找不到指定 QC Item 與原始因子組合的管制內容。");
        var result = Map(reader);
        if (reader.Read()) throw Rule(409, "指定 QC Item／因子組合存在歧義來源列，資料已保留。");
        return result;
    }

    private static void ValidateBodyItem(NpgsqlConnection connection, NpgsqlTransaction transaction, string no, string? bodyNo)
    {
        if (bodyNo is null) return;
        bodyNo = ValidateCode(bodyNo, "QCItemNo", 25, true);
        using var command = new NpgsqlCommand("SELECT lower(btrim(@body))=lower(btrim(@no));", connection, transaction);
        AddVarchar(command, "body", bodyNo); AddVarchar(command, "no", no);
        if (command.ExecuteScalar() is not true) throw Rule(400, "更新時不可變更 QCItemNo。");
    }

    private static Dictionary<string, string> ReadStoredFactors(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException();
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var folded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Name.Length is < 1 or > 30 || property.Name.Trim(' ').Length == 0 || property.Name.Any(char.IsControl) ||
                    property.Value.ValueKind != JsonValueKind.String || !folded.Add(property.Name.Trim(' '))) throw new JsonException();
                var value = property.Value.GetString()!;
                if (value.Length > 61 || value.Any(char.IsControl)) throw new JsonException();
                result.Add(property.Name, value);
            }
            if (result.Count == 0) throw new JsonException();
            return result;
        }
        catch (JsonException) { throw Rule(409, "來源管制內容的 JSONB 因子識別值無效或有歧義，請先整理來源資料。"); }
    }

    private static Qc202ControlLimitDto Returned(NpgsqlCommand command)
    {
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Conflict();
        var result = Map(reader);
        if (reader.Read()) throw Rule(409, "資料操作命中多筆來源列，交易已取消。");
        return result;
    }

    private static Qc202ControlLimitDto Map(NpgsqlDataReader reader) => new(reader.GetString(0), ReadStoredFactors(reader.GetString(1)),
        Text(reader, 2), Text(reader, 3), Text(reader, 4), Text(reader, 5), Text(reader, 6), Text(reader, 7),
        Text(reader, 8), Utc(reader, 9), Text(reader, 10), Utc(reader, 11), reader.IsDBNull(12) ? null : reader.GetDecimal(12));

    private static void BindLimit(NpgsqlCommand command, string no, Dictionary<string, string> factors, Limits limits, string? rules, string actor)
    {
        AddVarchar(command, "no", no); AddJson(command, "factors", factors);
        AddVarchar(command, "us", limits.Us); AddVarchar(command, "ls", limits.Ls); AddVarchar(command, "ucl", limits.Ucl);
        AddVarchar(command, "lcl", limits.Lcl); AddVarchar(command, "cl", limits.Cl);
        AddVarchar(command, "rules", rules); AddVarchar(command, "actor", actor);
    }

    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@table AND c.relkind IN ('r','p'));", connection, transaction);
        AddVarchar(command, "table", table);
        return command.ExecuteScalar() is true;
    }

    private static void RequireTable(NpgsqlConnection connection, NpgsqlTransaction transaction, string table, string owner)
    {
        if (!TableExists(connection, transaction, table)) throw Rule(503, $"{owner} 相依資料表尚未建立，無法完成操作。");
    }

    private static string ValidateCode(string? value, string field, int max, bool required)
    {
        var text = value?.Trim(' ') ?? "";
        if ((required && text.Length == 0) || text.Length > max || text.Any(char.IsControl))
            throw Rule(400, $"{field} {(required ? "為必填且" : "")}不可超過 {max} 個字元或含控制字元。");
        return text;
    }

    private static string ValidateActor(string actor)
    {
        var value = actor.Trim();
        if (value.Length is < 1 or > 10 || value.Any(char.IsControl)) throw Rule(400, "登入使用者編號長度無效，無法保存 audit 欄位。");
        return value;
    }

    private static string? Text(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static int? Integer(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    private static DateTime? Utc(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc);
    private static void AddVarchar(NpgsqlCommand command, string name, string? value) => command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;
    private static void AddNumeric(NpgsqlCommand command, string name, decimal? value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static void AddJson(NpgsqlCommand command, string name, Dictionary<string, string> value) => command.Parameters.Add(name, NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(value);
    private static void TryRollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { } }
    private static Qc202RuleException Rule(int status, string message) => new(status, message);
    private static Qc202RuleException Conflict() => Rule(409, "管制內容已被其他操作更新，請重新載入。");
}
