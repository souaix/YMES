using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc108Repository(NpgsqlDataSource dataSource)
{
    private const string Columns = "ruleserial,ruleno,rulename,functionname,ruletype,issuestate,creator,createdate,description,ruleoption,testoutof,testcount,comparevalue1,comparevalue2,relation,wetype,datastamp,reviser,revisedate";
    private static readonly string[] Fields = ["ruleName", "ruleType", "functionName", "description", "ruleOption", "testOutof", "testCount", "compareValue1", "compareValue2", "relation"];
    private static readonly string[] Comparisons = [">", ">=", "<", "<=", "=", "<>"];
    public static IReadOnlyList<Qc108RuleOption> RuleOptions { get; } = Array.AsReadOnly(new[]
    {
        new Qc108RuleOption(0, "Value"), new Qc108RuleOption(3, "Value Range"), new Qc108RuleOption(1, "Sigma"), new Qc108RuleOption(2, "Line"),
        new Qc108RuleOption(4, "Sigma Range"), new Qc108RuleOption(5, "Other"), new Qc108RuleOption(6, "Special Value")
    });

    public IReadOnlyList<Qc108RuleDto> List()
    {
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null);
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblqcrulebasis ORDER BY ruleserial,ruleno;", connection);
        var result = new List<Qc108RuleDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(Read(reader));
        return result;
    }

    public Qc108RuleDto Create(Qc108RuleWriteRequest request, string actor)
    {
        actor = Actor(actor);
        var no = Key(request.RuleNo);
        var value = Normalize(request, null);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            using (var check = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblqcrulebasis WHERE lower(ruleno)=lower(@no) OR lower(ruleserial)=lower(@no);", connection, transaction))
            {
                Text(check, "no", no, 30);
                if (check.ExecuteScalar() is not null) throw Rule(409, "RuleNo／RuleSerial 已存在。");
            }
            using var command = new NpgsqlCommand($"""
                INSERT INTO "YMES".tblqcrulebasis
                    (ruleserial,ruleno,rulename,functionname,ruletype,issuestate,creator,createdate,description,
                     ruleoption,testoutof,testcount,comparevalue1,comparevalue2,relation,wetype)
                VALUES (@serial,@no,@name,@function,@type,0,@actor,now(),@description,
                        @option,@outof,@count,@value1,@value2,@relation,0)
                RETURNING {Columns};
                """, connection, transaction);
            Text(command, "serial", no, 30); Text(command, "no", no, 30);
            Bind(command, value, actor);
            var result = Returned(command);
            transaction.Commit(); return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Rollback(transaction); throw Rule(409, "RuleNo／RuleSerial 已存在。");
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc108RuleDto Update(string ruleNo, Qc108RuleWriteRequest request, Qc108ExpectedVersion expected, string actor)
    {
        actor = Actor(actor);
        var no = Key(ruleNo);
        if (!string.Equals(no, Key(request.RuleNo), StringComparison.Ordinal)) throw Rule(400, "RuleNo 不可修改。");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = ReadLocked(connection, transaction, no);
            EnsureExpected(current, expected);
            EnsureManagedScope(current);
            if (current.IssueState != 0)
                throw Rule(409, current.IssueState == 1 ? "簽核中的法則不可修改。" : "已簽核／不使用法則的 Issue Control 欄位設定尚未接入，目前唯讀。");
            var value = Normalize(request, current);
            using var command = new NpgsqlCommand($"""
                UPDATE "YMES".tblqcrulebasis
                SET rulename=@name,functionname=@function,ruletype=@type,description=@description,
                    ruleoption=@option,testoutof=@outof,testcount=@count,comparevalue1=@value1,
                    comparevalue2=@value2,relation=@relation,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE ruleserial=@serial AND issuestate IS NOT DISTINCT FROM @state
                  AND datastamp IS NOT DISTINCT FROM @stamp
                RETURNING {Columns};
                """, connection, transaction);
            Bind(command, value, actor); BindExpected(command, current);
            var result = Returned(command);
            transaction.Commit(); return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc108RuleDto Delete(string ruleNo, Qc108ExpectedVersion expected, string actor)
    {
        actor = Actor(actor);
        var no = Key(ruleNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = ReadLocked(connection, transaction, no);
            EnsureExpected(current, expected);
            EnsureManagedScope(current);
            if (current.IssueState == 1) throw Rule(409, "簽核中的法則不可刪除。");
            Qc108RuleDto result;
            if (current.IssueState == 2) result = ChangeState(connection, transaction, current, actor, -1);
            else if (current.IssueState is 0 or -1)
            {
                EnsureNoReferences(connection, transaction, current.RuleNo);
                using var command = new NpgsqlCommand($"""
                    DELETE FROM "YMES".tblqcrulebasis
                    WHERE ruleserial=@serial AND issuestate IS NOT DISTINCT FROM @state
                      AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {Columns};
                    """, connection, transaction);
                BindExpected(command, current);
                result = Returned(command);
            }
            else throw Rule(409, "目前法則狀態不允許刪除。");
            transaction.Commit(); return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc108RuleDto Approve(string ruleNo, Qc108ExpectedVersion expected, string actor)
    {
        actor = Actor(actor);
        if (expected.IssueState != 0) throw Rule(400, "核准只接受未簽核狀態。");
        var no = Key(ruleNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = ReadLocked(connection, transaction, no);
            EnsureExpected(current, expected);
            EnsureManagedScope(current);
            // Validate the stored definition before publishing it. This is not
            // an implementation of the SPC rule execution/sampling engine.
            var definition = Normalize(new Qc108RuleWriteRequest
            {
                RuleNo = current.RuleNo, RuleName = current.RuleName, RuleType = current.RuleType,
                FunctionName = current.FunctionName, Description = current.Description, RuleOption = current.RuleOption,
                TestOutof = current.TestOutof, TestCount = current.TestCount, CompareValue1 = current.CompareValue1,
                CompareValue2 = current.CompareValue2, Relation = current.Relation
            }, current);
            if (current.RuleOption == 6 && (current.TestOutof != definition.Outof || current.TestCount != definition.Count))
                throw Rule(422, "Special Value 的既有數量與選項不一致，請先修改保存後再核准。");
            if (new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" }.Any(table => Exists(connection, transaction, table)))
                throw Rule(503, "YMES 已存在 Issue/Job 表；QC108 尚未接入共用簽核服務，資料未變更。");
            var result = ChangeState(connection, transaction, current, actor, 2);
            transaction.Commit(); return result;
        }
        catch { Rollback(transaction); throw; }
    }

    private static Qc108RuleDto ChangeState(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc108RuleDto current, string actor, int next)
    {
        using var command = new NpgsqlCommand($"""
            UPDATE "YMES".tblqcrulebasis
            SET issuestate=@next,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
            WHERE ruleserial=@serial AND issuestate IS NOT DISTINCT FROM @state
              AND datastamp IS NOT DISTINCT FROM @stamp RETURNING {Columns};
            """, connection, transaction);
        BindExpected(command, current); Number(command, "next", next); Text(command, "actor", actor, 10);
        return Returned(command);
    }

    private static Qc108RuleDto ReadLocked(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblqcrulebasis WHERE lower(ruleno)=lower(@no) ORDER BY ruleserial FOR UPDATE;", connection, transaction);
        Text(command, "no", no, 30);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, $"找不到計量品管法則 {no}。");
        var row = Read(reader);
        if (reader.Read()) throw Rule(409, "RuleNo 對應多個 RuleSerial，請先整理來源資料，不能任意修改其中一筆。");
        return row;
    }

    private static Qc108RuleDto Returned(NpgsqlCommand command)
    {
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Conflict();
        return Read(reader);
    }

    private static Qc108RuleDto Read(NpgsqlDataReader r)
    {
        var state = Int(r, 5);
        return new(r.GetString(0), r.GetString(1), r.GetString(2), String(r, 3), Int(r, 4), state, String(r, 6), Date(r, 7),
            String(r, 8), Int(r, 9), Int(r, 10), Int(r, 11), String(r, 12), String(r, 13), String(r, 14), Int(r, 15),
            r.IsDBNull(16) ? null : r.GetDecimal(16), String(r, 17), Date(r, 18), state == 0 ? Fields : []);
    }

    private sealed record Value(string Name, int Type, string? Function, string? Description, int? Option,
        int? Outof, int? Count, string? Value1, string? Value2, string? Relation);

    private static Value Normalize(Qc108RuleWriteRequest request, Qc108RuleDto? current)
    {
        var name = Required(request.RuleName, "RuleName", 50);
        var type = request.RuleType ?? current?.RuleType;
        if (type is not (0 or 1)) throw Rule(400, "RuleType 僅允許 0（System）或 1（User Define）。");
        if (type == 1) throw Rule(503, "尚未取得有效 FUNQR 函式目錄，User Define 新增、修改與核准暫不可用；不接受 DEMO 函式名稱。");
        var option = request.RuleOption ?? current?.RuleOption;
        if (option is null || (option is not (0 or 1 or 2 or 3 or 4 or 5 or 6 or 9))) throw Rule(400, "RuleOption 必須選擇有效 System 法則。");
        if (option == 9 && current?.RuleOption != 9) throw Rule(400, "Other=9 僅保留既有資料；新建或改選 Other 請使用 5。");
        // Source sparse update: blank or irrelevant fields retain their old
        // values. Switching option does not erase dormant comparison fields.
        var description = Optional(request.Description, "Description", 255, true) ?? current?.Description;
        var value1 = current?.CompareValue1;
        var value2 = current?.CompareValue2;
        var count = request.TestCount ?? current?.TestCount;
        var outof = request.TestOutof ?? current?.TestOutof;
        var relation = Optional(request.Relation, "Relation", 10) ?? current?.Relation;
        if (option is 0 or 1 or 4)
        {
            value1 = NumericText(request.CompareValue1, current?.CompareValue1, "CompareValue1");
            relation = Choice(relation, Comparisons, "Relation");
        }
        else if (option == 3)
        {
            value1 = NumericText(request.CompareValue1, current?.CompareValue1, "CompareValue1");
            value2 = NumericText(request.CompareValue2, current?.CompareValue2, "CompareValue2");
            if (Parse(value1) > Parse(value2)) throw Rule(400, "Value Range 的下限不得大於上限。");
            relation = Choice(relation, ["Between", "Beyond"], "Relation");
        }
        else if (option == 2)
        {
            value1 = Choice(Optional(request.CompareValue1, "CompareValue1", 10) ?? current?.CompareValue1, ["US", "UCL", "CL", "LCL", "LS"], "Line");
            relation = Choice(relation, Comparisons, "Relation");
        }
        else if (option is 5 or 9) relation = Choice(relation, ["Increase", "Decrease", "Alternate"], "Relation");
        else if (option == 6)
        {
            value1 = Choice(Optional(request.CompareValue1, "CompareValue1", 10) ?? current?.CompareValue1,
                ["CpkGoal", "US", "UCL", "CL", "LCL", "LS", "SpecLimit"], "Special Value");
            relation = Choice(relation, value1 == "SpecLimit" ? ["Between", "Beyond"] : [">", ">=", "<", "<=", "<>"], "Relation");
            if (value1 == "CpkGoal") { count = 1; outof = 1; }
            else outof = count;
        }
        var validatedOutof = Count(outof, "TestOutof");
        var validatedCount = Count(count, "TestCount");
        if (validatedOutof < validatedCount) throw Rule(400, "TestOutof 必須大於或等於 TestCount。");
        return new(name, 0, current?.FunctionName, description, option, validatedOutof, validatedCount, value1, value2, relation);
    }

    private static string NumericText(string? input, string? original, string field)
    {
        var text = Optional(input, field, 10) ?? original;
        if (string.IsNullOrEmpty(text) || !TryFiniteNumber(text, out _))
            throw Rule(400, $"{field} 為必填，且必須是 10 字元內的有效數字。");
        return text;
    }
    private static double Parse(string text) => TryFiniteNumber(text, out var value) ? value : throw Rule(400, "比較值必須為有效數字。");
    private static bool TryFiniteNumber(string text, out double value)
    {
        // The Vue form validates Number(text), which is an IEEE-754 double.
        // Preserve exponent underflow/rounding and the existing radix syntax
        // instead of imposing an undocumented decimal range on varchar data.
        var radix = text.Length > 2 && text[0] == '0' ? char.ToLowerInvariant(text[1]) switch
        {
            'x' => 16, 'o' => 8, 'b' => 2, _ => 0
        } : 0;
        if (radix != 0)
        {
            value = 0;
            foreach (var character in text.AsSpan(2))
            {
                var digit = character is >= '0' and <= '9' ? character - '0'
                    : character is >= 'a' and <= 'f' ? character - 'a' + 10
                    : character is >= 'A' and <= 'F' ? character - 'A' + 10 : -1;
                if (digit < 0 || digit >= radix) return false;
                value = value * radix + digit;
            }
            return double.IsFinite(value);
        }
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
    }
    private static int Count(decimal? value, string field)
    {
        if (!value.HasValue || value.Value < 1 || value.Value > 99 || value.Value != decimal.Truncate(value.Value))
            throw Rule(400, $"{field} 必須是 1 到 99 的整數。");
        return decimal.ToInt32(value.Value);
    }
    private static string Choice(string? value, string[] choices, string field) =>
        choices.FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) ?? throw Rule(400, $"{field} 必須選擇有效選項。");

    private static void EnsureNoReferences(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        foreach (var table in new[] { "tblqcitemrule", "tblspcqcchartrule", "tblspcattlimit" })
        {
            if (!Exists(connection, transaction, table)) continue;
            // Child write paths lock their referenced master rows FOR SHARE
            // before writing, in the same transaction. ReadLocked's FOR UPDATE
            // therefore serializes this check/delete with those child writes.
            var predicate = table == "tblspcattlimit"
                ? "EXISTS (SELECT 1 FROM unnest(string_to_array(COALESCE(ruleset,''),';')) AS entry(value) WHERE lower(btrim(split_part(entry.value,',',1)))=lower(@no))"
                : "lower(ruleno)=lower(@no)";
            using var command = new NpgsqlCommand($"SELECT 1 FROM \"YMES\".{table} WHERE {predicate} LIMIT 1;", connection, transaction);
            Text(command, "no", no, 30);
            if (command.ExecuteScalar() is not null) throw Rule(409, "此法則已被品管項目或管制圖引用，不能實體刪除；引用資料未變更。");
        }
    }

    private static void EnsureExpected(Qc108RuleDto current, Qc108ExpectedVersion expected)
    {
        if (current.IssueState != expected.IssueState || current.DataStamp != expected.DataStamp) throw Conflict();
    }
    private static void EnsureManagedScope(Qc108RuleDto current)
    {
        if (current.RuleType == 2 || current.WeType != 0)
            throw Rule(409, "QC108 僅維護 RuleType 非 2 且 WEType=0 的計量品管法則。");
    }
    private static string Key(string? value) => Required(value, "RuleNo", 30).ToUpperInvariant();
    private static string Required(string? value, string field, int max) => Optional(value, field, max) ?? throw Rule(400, $"{field} 為必填。");
    private static string? Optional(string? value, string field, int max, bool multiline = false)
    {
        var text = (value ?? "").Trim();
        if (text.Length > max || text.Any(c => char.IsControl(c) && (!multiline || c is not ('\r' or '\n' or '\t'))))
            throw Rule(400, $"{field} 最長 {max} 個字元且不可含無效控制字元。");
        return text.Length == 0 ? null : text;
    }
    private static string Actor(string? actor) => Required(actor, "登入帳號", 10);
    private static bool Exists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@table AND c.relkind IN ('r','p','v','m','f'));", connection, transaction);
        Text(command, "table", table, 63); return command.ExecuteScalar() is true;
    }
    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction)
    {
        if (!Exists(connection, transaction, "tblqcrulebasis")) throw Rule(503, "YMES 尚未建立 TBLQCRULEBASIS，請先完成 QC108 migration。");
    }
    private static void Bind(NpgsqlCommand command, Value value, string actor)
    {
        Text(command, "name", value.Name, 50); Number(command, "type", value.Type); Text(command, "function", value.Function, 50);
        Text(command, "description", value.Description, 255); Number(command, "option", value.Option); Number(command, "outof", value.Outof);
        Number(command, "count", value.Count); Text(command, "value1", value.Value1, 10); Text(command, "value2", value.Value2, 10);
        Text(command, "relation", value.Relation, 10); Text(command, "actor", actor, 10);
    }
    private static void BindExpected(NpgsqlCommand command, Qc108RuleDto current)
    {
        Text(command, "serial", current.RuleSerial, 30); Number(command, "state", current.IssueState); Number(command, "stamp", current.DataStamp);
    }
    private static void Text(NpgsqlCommand command, string name, string? value, int size) => command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = (object?)value ?? DBNull.Value;
    private static void Number(NpgsqlCommand command, string name, decimal? value) => command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static string? String(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);
    private static int? Int(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : Convert.ToInt32(r.GetValue(i));
    private static DateTime? Date(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetDateTime(i);
    private static void Rollback(NpgsqlTransaction t) { try { t.Rollback(); } catch { /* preserve original failure */ } }
    private static Qc108RuleException Rule(int status, string message) => new(status, message);
    private static Qc108RuleException Conflict() => Rule(409, "法則狀態或版本已變更，請重新載入後再試。");
}
