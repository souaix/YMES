using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc302Repository(NpgsqlDataSource dataSource)
{
    private const long MaxWorkbookBytes = 20L * 1024 * 1024;
    private const int MaxImportRows = 500;
    private const int MaxPreviewRows = MaxImportRows;
    private const int MaxColumns = 128;
    private const int MaxHeaderLength = 128;

    private static readonly string[] SystemColumns = ["EXECUTE", "FACTORSERIAL", "SELECT", "RESULT"];
    private static readonly string[] RuleColumns = ["TRule", "XRule", "RRule", "SRule", "MRule"];
    private static readonly string[] NumericColumns =
    [
        "TUS", "TCS", "TLS", "UCL", "CL", "LCL", "Target", "CPKGoal", "TYAxisTo", "TYAxisFrom",
        "XUS", "XCS", "XLS", "XUCL", "XCL", "XLCL", "XYAxisTo", "XYAxisFrom",
        "RUS", "RCS", "RLS", "RUCL", "RCL", "RLCL", "RYAxisTo", "RYAxisFrom",
        "SUS", "SCS", "SLS", "SUCL", "SCL", "SLCL", "SYAxisTo", "SYAxisFrom",
        "MUS", "MCS", "MLS", "MUCL", "MCL", "MLCL", "MYAxisTo", "MYAxisFrom"
    ];

    private static readonly ChartMap[] Charts =
    [
        new(0, "T", true), new(1, "X", false), new(2, "R", false), new(3, "S", false), new(4, "M", false)
    ];

    public Qc302PreviewResponse Preview(IFormFile? file)
    {
        ValidateWorkbook(file);
        var extension = Path.GetExtension(file!.FileName);
        if (!extension.Equals(".xls", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new Qc302RuleException(StatusCodes.Status415UnsupportedMediaType, "管制圖匯入只允許 .xls 或 .xlsx。 ");

        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var input = file.OpenReadStream();
            using var workbook = ExcelReaderFactory.CreateReader(input);
            if (!workbook.Read()) throw new Qc302RuleException(400, "工作簿沒有第一列欄名。 ");
            if (workbook.FieldCount is < 1 or > MaxColumns)
                throw new Qc302RuleException(400, $"欄數必須介於 1 至 {MaxColumns} 欄。 ");

            var originalHeaders = Enumerable.Range(0, workbook.FieldCount)
                .Select(index => CellText(workbook.GetValue(index)))
                .Select((header, index) => header.Length == 0 ? $"unknow{index}" : header)
                .ToArray();
            if (originalHeaders.Any(header => header.Length > MaxHeaderLength))
                throw new Qc302RuleException(400, $"欄名不可超過 {MaxHeaderLength} 個字元。 ");
            var duplicate = originalHeaders.Where(header => !IsSystemColumn(header))
                .GroupBy(header => header, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null) throw new Qc302RuleException(400, $"欄名重複：{duplicate.Key}。");
            var columns = originalHeaders.Where(header => !IsSystemColumn(header)).ToArray();

            // Row 2 is the legacy sample/example row and is intentionally ignored.
            _ = workbook.Read();
            var rows = new List<IReadOnlyDictionary<string, string>>();
            while (workbook.Read())
            {
                var values = Enumerable.Range(0, originalHeaders.Length)
                    .Select(index => index < workbook.FieldCount ? CellText(workbook.GetValue(index)) : "")
                    .ToArray();
                if (values.All(string.IsNullOrWhiteSpace)) break;
                if (rows.Count >= MaxPreviewRows)
                    throw new Qc302RuleException(400, $"單次最多預覽 {MaxPreviewRows} 筆資料。 ");
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < originalHeaders.Length; index++)
                    if (!IsSystemColumn(originalHeaders[index])) row.Add(originalHeaders[index], values[index].Trim());
                rows.Add(row);
            }

            return new Qc302PreviewResponse(columns, rows);
        }
        catch (Qc302RuleException) { throw; }
        catch (Exception ex) when (ex is InvalidDataException or IOException or ArgumentException or FormatException or NotSupportedException or System.Xml.XmlException
                                   || ex.GetType().Namespace == "ExcelDataReader.Exceptions")
        {
            throw new Qc302RuleException(400, "無法讀取 Excel；請確認檔案有效、未加密且第一張工作表格式正確。 ");
        }
    }

    public IReadOnlyList<Qc302ImportRowResult> Import(Qc302ImportRequest request, string actor)
    {
        if (request.Rows is null) throw new Qc302RuleException(400, "rows 不可為空。 ");
        if (request.Rows.Count > MaxImportRows) throw new Qc302RuleException(400, $"單次最多匯入 {MaxImportRows} 列。 ");
        if (string.IsNullOrWhiteSpace(actor) || actor.Trim().Length > 10)
            throw new Qc302RuleException(400, "登入帳號不可空白且不可超過 10 個字元。 ");

        var results = new List<Qc302ImportRowResult>(request.Rows.Count);
        for (var index = 0; index < request.Rows.Count; index++)
        {
            var rowNo = index + 1;
            var qcItemNo = "";
            try
            {
                var row = NormalizeRequestRow(request.Rows[index]);
                qcItemNo = Get(row, "QCItemNo");
                var factorSerial = ImportOne(row, actor.Trim());
                results.Add(new Qc302ImportRowResult(rowNo, qcItemNo, true, "success", factorSerial));
            }
            catch (Qc302RuleException ex)
            {
                results.Add(new Qc302ImportRowResult(rowNo, qcItemNo, false, ex.Message, ""));
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                results.Add(new Qc302ImportRowResult(rowNo, qcItemNo, false, "QCItem/Factor 要因組合或圖表鍵已存在；該列未寫入。", ""));
            }
            catch (PostgresException)
            {
                results.Add(new Qc302ImportRowResult(rowNo, qcItemNo, false, "資料庫操作失敗；該列交易已回復。", ""));
            }
        }
        return results;
    }

    private string ImportOne(Dictionary<string, string> row, string actor)
    {
        var qcItemNo = RequiredText(row, "QCItemNo", 25);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            EnsureTable(connection, transaction, "tblqcitemforvariables", "TBLQCITEMFORVARIABLES 品管項目主檔");
            EnsureTable(connection, transaction, "tblqcitemfactor", "TBLQCITEMFACTOR 品管要因設定");
            EnsureTable(connection, transaction, "tblspcqcchartbasis", "QC302 圖表主檔 migration 0024");
            EnsureTable(connection, transaction, "tblspcqcchartdetail", "QC302 圖表明細 migration 0024");
            EnsureTable(connection, transaction, "tblspcqcchartdetaillog", "QC302 圖表明細歷程 migration 0024");
            EnsureTable(connection, transaction, "tblspcqcchartrule", "QC302 圖表法則 migration 0024");

            var item = ReadApprovedQcItem(connection, transaction, qcItemNo);
            qcItemNo = item.QcItemNo;
            var factors = ReadOnlineFactors(connection, transaction, qcItemNo);
            var factorValues = ReadFactorValues(row, factors);
            var sampleSize = ResolveSampleSize(row, item.SampleSize);
            var details = BuildDetails(row, item.SpecLineType);
            ValidateBasisFields(row);
            var rulesBySpc = ParseRules(row);

            var factorJson = JsonSerializer.Serialize(factorValues);
            // Same canonical parent + same jsonb factor object share one mutex,
            // regardless of HTTP item casing, JSON key order or delimiters.
            using (var advisory = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('YMES:QC302:ChartFactors:' || jsonb_build_array(lower(@item),@factors::jsonb)::text,0));", connection, transaction))
            {
                AddText(advisory, "item", qcItemNo, 25);
                advisory.Parameters.Add("factors", NpgsqlDbType.Jsonb).Value = factorJson;
                advisory.ExecuteNonQuery();
            }

            var matching = FindChartSerials(connection, transaction, qcItemNo, factorJson);
            if (matching.Count > 1)
                throw new Qc302RuleException(409, "QCChartBasis Duplicate：相同 QCItemNo 與線上因子組合有多筆管制圖。 ");
            var isNew = matching.Count == 0;
            var factorSerial = isNew ? $"FS{Guid.NewGuid():N}" : matching[0];

            List<ChartRule> defaultRules = [];
            if (isNew)
                defaultRules = ReadDefaultRules(connection, transaction, qcItemNo);
            var approvedRules = rulesBySpc.Count == 0
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : ReadApprovedRules(connection, transaction);
            ValidateRules(rulesBySpc, approvedRules);

            ValidatePlanIfProvided(connection, transaction, item, Get(row, "PlanNo"));
            UpsertBasis(connection, transaction, isNew, factorSerial, qcItemNo, actor, sampleSize, factorJson, row);
            UpsertDetails(connection, transaction, factorSerial, actor, details);
            if (isNew) InsertChartRules(connection, transaction, factorSerial, defaultRules);
            ReplaceProvidedRules(connection, transaction, factorSerial, rulesBySpc);

            transaction.Commit();
            return factorSerial;
        }
        catch
        {
            try { transaction.Rollback(); } catch { /* preserve the row failure */ }
            throw;
        }
    }

    private static List<string> FindChartSerials(NpgsqlConnection connection, NpgsqlTransaction transaction, string qcItemNo, string factorJson)
    {
        using var command = new NpgsqlCommand("SELECT factorserial FROM \"YMES\".tblspcqcchartbasis WHERE lower(qcitemno)=lower(@no) AND factorvalues=@factors ORDER BY factorserial FOR UPDATE;", connection, transaction);
        AddText(command, "no", qcItemNo, 25);
        command.Parameters.Add("factors", NpgsqlDbType.Jsonb).Value = factorJson;
        using var reader = command.ExecuteReader();
        var serials = new List<string>();
        while (reader.Read()) serials.Add(reader.GetString(0));
        return serials;
    }

    private static QcItem ReadApprovedQcItem(NpgsqlConnection connection, NpgsqlTransaction transaction, string qcItemNo)
    {
        using var command = new NpgsqlCommand("""
            SELECT qcitemno,samplesize::int,speclinetype::int,componentcollect::int,issuestate::int
            FROM "YMES".tblqcitemforvariables
            WHERE lower(qcitemno)=lower(@no)
            ORDER BY lower(qcitemno),qcitemno
            FOR SHARE;
            """, connection, transaction);
        AddText(command, "no", qcItemNo, 25);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new Qc302RuleException(400, $"計量 QC Item {qcItemNo} 不存在或尚未核准（IssueState 必須為 2）。");
        var canonical = reader.GetString(0);
        if (reader.IsDBNull(2)) throw new Qc302RuleException(400, $"QC Item {canonical} 未設定 SpecLineType。");
        var item = new QcItem(canonical, reader.IsDBNull(1) ? null : reader.GetInt32(1), reader.GetInt32(2), reader.IsDBNull(3) ? null : reader.GetInt32(3));
        var approved = !reader.IsDBNull(4) && reader.GetInt32(4) == 2;
        if (reader.Read()) throw new Qc302RuleException(409, "QC Item 存在大小寫不同的重複主檔，無法安全決定匯入來源。");
        if (!approved) throw new Qc302RuleException(400, $"計量 QC Item {canonical} 尚未核准（IssueState 必須為 2）。");
        return item;
    }

    private static List<OnlineFactor> ReadOnlineFactors(NpgsqlConnection connection, NpgsqlTransaction transaction, string qcItemNo)
    {
        using var command = new NpgsqlCommand("""
            SELECT factorno, factorspec
            FROM "YMES".tblqcitemfactor
            WHERE lower(qcitemno)=lower(@no) AND upper(factoronline)='Y'
            ORDER BY factorno;
            """, connection, transaction);
        AddText(command, "no", qcItemNo, 25);
        using var reader = command.ExecuteReader();
        var factors = new List<OnlineFactor>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            var no = reader.GetString(0);
            if (!names.Add(no))
                throw new Qc302RuleException(409, "QC Item 的線上 FactorNo 存在大小寫歧義，請先整理來源因子；本列未匯入。");
            factors.Add(new OnlineFactor(no, reader.GetString(1)));
        }
        return factors;
    }

    private static Dictionary<string, string> ReadFactorValues(Dictionary<string, string> row, IReadOnlyList<OnlineFactor> factors)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var supplied = row.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var factor in factors)
        {
            if (!row.TryGetValue(factor.FactorNo, out var value) || string.IsNullOrWhiteSpace(value))
                throw new Qc302RuleException(400, $"線上因子 {factor.FactorNo} 必須提供非空值。 ");
            if (value.Length > 61) throw new Qc302RuleException(400, $"線上因子 {factor.FactorNo} 不可超過 61 個字元。 ");
            if (string.Equals(factor.FactorSpec, "Y", StringComparison.OrdinalIgnoreCase) && value == "*")
                throw new Qc302RuleException(400, $"特定值因子 {factor.FactorNo} 不允許使用 *。 ");
            values.Add(factor.FactorNo, value.Trim());
        }

        var allowedStatic = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "QCItemNo", "SampleSize", "Creator", "CreateDate", "ChartTitle", "KeyNode", "Sponsor", "PlanNo", "ChartUnit", "MEMO", "PRODUCT", "MATERIAL" }
            .Concat(NumericColumns).Concat(RuleColumns).Concat(SystemColumns).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = supplied.FirstOrDefault(name => !allowedStatic.Contains(name) && !factors.Any(f => string.Equals(f.FactorNo, name, StringComparison.OrdinalIgnoreCase)));
        if (unknown is not null && !string.IsNullOrWhiteSpace(Get(row, unknown)))
            throw new Qc302RuleException(400, $"欄位 {unknown} 不是此 QC Item 核准的線上因子，已拒絕匯入。 ");

        return values;
    }

    private static int ResolveSampleSize(Dictionary<string, string> row, int? approvedSampleSize)
    {
        var value = Get(row, "SampleSize");
        if (string.IsNullOrWhiteSpace(value))
        {
            if (approvedSampleSize is null or <= 0) throw new Qc302RuleException(400, "SampleSize 空白且 QC Item 沒有有效的核准樣本數。 ");
            return approvedSampleSize.Value;
        }
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed is <= 0 or > 99999)
            throw new Qc302RuleException(400, "SampleSize 必須是 1 至 99999 的整數。 ");
        return parsed;
    }

    private static List<ChartDetail> BuildDetails(Dictionary<string, string> row, int specLineType)
    {
        if (specLineType is < 0 or > 2) throw new Qc302RuleException(400, "QC Item SpecLineType 僅支援 0、1、2。 ");
        var tus = Get(row, "TUS");
        var tls = Get(row, "TLS");
        if (specLineType == 0 && (tus.Length == 0 || tls.Length == 0))
            throw new Qc302RuleException(400, "雙邊規格（SpecLineType=0）必須同時提供 TUS 與 TLS。 ");
        if (specLineType == 1 && tus.Length == 0) throw new Qc302RuleException(400, "上限規格（SpecLineType=1）必須提供 TUS。 ");
        if (specLineType == 2 && tls.Length == 0) throw new Qc302RuleException(400, "下限規格（SpecLineType=2）必須提供 TLS。 ");

        var numbers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in NumericColumns)
        {
            var text = Get(row, field);
            if (text.Length == 0) continue;
            if (text.Length > (field.Contains("YAxis", StringComparison.OrdinalIgnoreCase) || field.Equals("CPKGoal", StringComparison.OrdinalIgnoreCase) || field.Equals("Target", StringComparison.OrdinalIgnoreCase) ? 10 : 15) ||
                !decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new Qc302RuleException(400, $"{field} 必須是有效數值且符合欄位長度。 ");
            numbers[field] = value;
        }

        foreach (var chart in Charts)
        {
            var prefix = chart.Prefix;
            var orderedFields = chart.Trend
                ? new[] { "TUS", "UCL", "CL", "LCL", "TLS" }
                : new[] { $"{prefix}US", $"{prefix}UCL", $"{prefix}CL", $"{prefix}LCL", $"{prefix}LS" };
            decimal? previous = null;
            foreach (var field in orderedFields)
            {
                if (!numbers.TryGetValue(field, out var value)) continue;
                if (previous is not null && previous < value)
                    throw new Qc302RuleException(400, $"{ChartName(chart.SpcId)}界限需由大至小排列（{field}）。 ");
                previous = value;
            }
            var fromField = chart.Trend ? "TYAxisFrom" : $"{prefix}YAxisFrom";
            var toField = chart.Trend ? "TYAxisTo" : $"{prefix}YAxisTo";
            if (numbers.TryGetValue(fromField, out var from) && numbers.TryGetValue(toField, out var to) && from >= to)
                throw new Qc302RuleException(400, $"{ChartName(chart.SpcId)} Y 軸下限必須小於上限。 ");
        }

        return Charts.Select(chart =>
        {
            var prefix = chart.Prefix;
            var us = Get(row, chart.Trend ? "TUS" : $"{prefix}US");
            var ls = Get(row, chart.Trend ? "TLS" : $"{prefix}LS");
            var ucl = Get(row, chart.Trend ? "UCL" : $"{prefix}UCL");
            var lcl = Get(row, chart.Trend ? "LCL" : $"{prefix}LCL");
            var cl = Get(row, chart.Trend ? "CL" : $"{prefix}CL");
            var cs = Get(row, chart.Trend ? "TCS" : $"{prefix}CS");
            return new ChartDetail(chart.SpcId, us, ls, ucl, lcl, cl,
                chart.Trend ? Get(row, "Target") : "", chart.Trend ? Get(row, "CPKGoal") : "",
                Get(row, chart.Trend ? "TYAxisFrom" : $"{prefix}YAxisFrom"),
                Get(row, chart.Trend ? "TYAxisTo" : $"{prefix}YAxisTo"), cs);
        }).ToList();
    }

    private static void ValidateBasisFields(Dictionary<string, string> row)
    {
        foreach (var (field, size) in new[]
        {
            ("ChartTitle", 50), ("KeyNode", 1), ("Sponsor", 50), ("PlanNo", 25), ("ChartUnit", 30), ("MEMO", 255), ("PRODUCT", 61), ("MATERIAL", 61)
        })
            if (Get(row, field).Length > size) throw new Qc302RuleException(400, $"{field} 不可超過 {size} 個字元。 ");
        var keyNode = Get(row, "KeyNode");
        if (keyNode.Length > 0 && keyNode is not ("Y" or "N")) throw new Qc302RuleException(400, "KeyNode 僅允許 Y 或 N。 ");
    }

    private static Dictionary<int, List<ChartRule>> ParseRules(Dictionary<string, string> row)
    {
        var parsed = new Dictionary<int, List<ChartRule>>();
        for (var index = 0; index < RuleColumns.Length; index++)
        {
            var text = Get(row, RuleColumns[index]);
            if (string.IsNullOrWhiteSpace(text)) continue;
            var rules = new List<ChartRule>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in text.Split(';', StringSplitOptions.TrimEntries))
            {
                if (entry.Length == 0) throw new Qc302RuleException(400, $"{RuleColumns[index]} 含空白法則項目。 ");
                var parts = entry.Split(',', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2 || parts[0].Length is 0 or > 30)
                    throw new Qc302RuleException(400, $"{RuleColumns[index]} 格式需為 RuleNo,Action，並以分號分隔。 ");
                if (!seen.Add(parts[0])) throw new Qc302RuleException(400, $"{RuleColumns[index]} 有重複 RuleNo：{parts[0]}。 ");
                var action = parts[1].ToLowerInvariant() switch
                {
                    "warning" or "0" => 0, "wait" or "1" => 1, "hold" or "2" => 2, "alert" or "3" => 3,
                    _ => throw new Qc302RuleException(400, $"{RuleColumns[index]} 的 Action 僅允許 warning、wait、hold、alert 或 0–3。 ")
                };
                rules.Add(new ChartRule(index, parts[0], action, null, null, null));
            }
            parsed[index] = rules;
        }
        return parsed;
    }

    private static List<ChartRule> ReadDefaultRules(NpgsqlConnection connection, NpgsqlTransaction transaction, string qcItemNo)
    {
        EnsureTable(connection, transaction, "tblqcitemrule", "TBLQCITEMRULE（QC Item 管制法則對應）");
        EnsureTable(connection, transaction, "tblqcrulebasis", "TBLQCRULEBASIS（核准管制法則）");
        using var command = new NpgsqlCommand("""
            SELECT m.spcid::int,m.ruleno,m.action::int,m.equipmentstate::int,m.erfstate::int,m.emailstate::int
            FROM "YMES".tblqcitemrule m
            JOIN "YMES".tblqcrulebasis b ON b.ruleno=m.ruleno AND b.issuestate=2
            WHERE lower(m.qcitemno)=lower(@no)
            ORDER BY b.ruleserial,m.spcid,m.ruleno
            FOR SHARE OF b;
            """, connection, transaction);
        AddText(command, "no", qcItemNo, 25);
        using var reader = command.ExecuteReader();
        var rules = new List<ChartRule>();
        while (reader.Read()) rules.Add(new ChartRule(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2),
            NullableInt(reader, 3), NullableInt(reader, 4), NullableInt(reader, 5)));
        return rules;
    }

    private static HashSet<string> ReadApprovedRules(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        EnsureTable(connection, transaction, "tblqcrulebasis", "TBLQCRULEBASIS（核准管制法則）");
        using var command = new NpgsqlCommand("SELECT ruleno FROM \"YMES\".tblqcrulebasis WHERE issuestate=2 ORDER BY ruleserial FOR SHARE;", connection, transaction);
        using var reader = command.ExecuteReader();
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read()) result.Add(reader.GetString(0));
        return result;
    }

    private static void ValidateRules(Dictionary<int, List<ChartRule>> rules, HashSet<string> approved)
    {
        foreach (var (spcId, items) in rules)
            foreach (var item in items)
                if (!approved.Contains(item.RuleNo))
                    throw new Qc302RuleException(400, $"{RuleColumns[spcId]} 的 RuleNo {item.RuleNo} 不存在或尚未核准。 ");
    }

    private static void ValidatePlanIfProvided(NpgsqlConnection connection, NpgsqlTransaction transaction, QcItem item, string planNo)
    {
        if (planNo.Length == 0) return;
        // The approved QC Item was already locked FOR SHARE in this row's
        // transaction. Preserve the existing Excel contract (no invented plan
        // parameter import), but validate/lock the correct real plan namespace.
        _ = QcSamplingPlanReference.ReadGeneral(connection, transaction, planNo, item.ComponentCollect, 2,
            (status, message) => new Qc302RuleException(status, message));
    }

    private static void UpsertBasis(NpgsqlConnection connection, NpgsqlTransaction transaction, bool isNew,
        string factorSerial, string qcItemNo, string actor, int sampleSize, string factorJson, Dictionary<string, string> row)
    {
        if (isNew)
        {
            using var insert = new NpgsqlCommand("""
                INSERT INTO "YMES".tblspcqcchartbasis
                    (factorserial,qcitemno,creator,createdate,samplesize,issuestate,keynode,sponsor,charttitle,
                     planno,chartunit,memo,product,material,factorvalues,datastamp)
                VALUES
                    (@serial,@qcitem,@actor,now(),@sample,2,COALESCE(NULLIF(@keynode,''),'N'),
                     NULLIF(@sponsor,''),NULLIF(@title,''),NULLIF(@plan,''),NULLIF(@unit,''),NULLIF(@memo,''),
                     COALESCE(NULLIF(@product,''),'*'),COALESCE(NULLIF(@material,''),'*'),@factors,0);
                """, connection, transaction);
            AddText(insert, "serial", factorSerial, 50); AddText(insert, "qcitem", qcItemNo, 25); AddText(insert, "actor", actor, 10);
            AddNumeric(insert, "sample", sampleSize); AddText(insert, "keynode", Get(row, "KeyNode"), 1);
            AddText(insert, "sponsor", Get(row, "Sponsor"), 50); AddText(insert, "title", Get(row, "ChartTitle"), 50);
            AddText(insert, "plan", Get(row, "PlanNo"), 25); AddText(insert, "unit", Get(row, "ChartUnit"), 30);
            AddText(insert, "memo", Get(row, "MEMO"), 255); AddText(insert, "product", Get(row, "PRODUCT"), 61);
            AddText(insert, "material", Get(row, "MATERIAL"), 61); insert.Parameters.Add("factors", NpgsqlDbType.Jsonb).Value = factorJson;
            insert.ExecuteNonQuery();
            return;
        }

        using var update = new NpgsqlCommand("""
            UPDATE "YMES".tblspcqcchartbasis SET
                samplesize=@sample,issuestate=2,factorvalues=@factors,
                keynode=COALESCE(NULLIF(@keynode,''),keynode),sponsor=COALESCE(NULLIF(@sponsor,''),sponsor),
                charttitle=COALESCE(NULLIF(@title,''),charttitle),planno=COALESCE(NULLIF(@plan,''),planno),
                chartunit=COALESCE(NULLIF(@unit,''),chartunit),memo=COALESCE(NULLIF(@memo,''),memo),
                product=COALESCE(NULLIF(@product,''),product),material=COALESCE(NULLIF(@material,''),material),
                reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
            WHERE factorserial=@serial;
            """, connection, transaction);
        AddText(update, "serial", factorSerial, 50); AddText(update, "actor", actor, 10); AddNumeric(update, "sample", sampleSize);
        AddText(update, "keynode", Get(row, "KeyNode"), 1); AddText(update, "sponsor", Get(row, "Sponsor"), 50);
        AddText(update, "title", Get(row, "ChartTitle"), 50); AddText(update, "plan", Get(row, "PlanNo"), 25);
        AddText(update, "unit", Get(row, "ChartUnit"), 30); AddText(update, "memo", Get(row, "MEMO"), 255);
        AddText(update, "product", Get(row, "PRODUCT"), 61); AddText(update, "material", Get(row, "MATERIAL"), 61);
        update.Parameters.Add("factors", NpgsqlDbType.Jsonb).Value = factorJson;
        if (update.ExecuteNonQuery() != 1) throw new Qc302RuleException(409, "管制圖已被其他作業修改，請重新匯入。 ");
    }

    private static void UpsertDetails(NpgsqlConnection connection, NpgsqlTransaction transaction, string factorSerial, string actor, IReadOnlyList<ChartDetail> details)
    {
        var startTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        foreach (var detail in details)
        {
            using (var command = new NpgsqlCommand("""
                INSERT INTO "YMES".tblspcqcchartdetail
                    (factorserial,spcid,us,ls,ucl,lcl,cl,target,cpkgoal,yaxisfrom,yaxisto,cs)
                VALUES (@serial,@spcid,NULLIF(@us,''),NULLIF(@ls,''),NULLIF(@ucl,''),NULLIF(@lcl,''),NULLIF(@cl,''),
                        NULLIF(@target,''),NULLIF(@cpk,''),NULLIF(@from,''),NULLIF(@to,''),NULLIF(@cs,''))
                ON CONFLICT (factorserial,spcid) DO UPDATE SET
                    us=EXCLUDED.us,ls=EXCLUDED.ls,ucl=EXCLUDED.ucl,lcl=EXCLUDED.lcl,cl=EXCLUDED.cl,
                    target=EXCLUDED.target,cpkgoal=EXCLUDED.cpkgoal,yaxisfrom=EXCLUDED.yaxisfrom,yaxisto=EXCLUDED.yaxisto,cs=EXCLUDED.cs;
                """, connection, transaction))
            {
                AddDetailParameters(command, factorSerial, detail);
                command.ExecuteNonQuery();
            }
            using var history = new NpgsqlCommand("""
                INSERT INTO "YMES".tblspcqcchartdetaillog
                    (factorserial,spcid,us,ls,ucl,lcl,cl,creator,starttime,target,cpkgoal,yaxisfrom,yaxisto,cs)
                VALUES (@serial,@spcid,NULLIF(@us,''),NULLIF(@ls,''),NULLIF(@ucl,''),NULLIF(@lcl,''),NULLIF(@cl,''),
                        @actor,@time,NULLIF(@target,''),NULLIF(@cpk,''),NULLIF(@from,''),NULLIF(@to,''),NULLIF(@cs,''));
                """, connection, transaction);
            AddDetailParameters(history, factorSerial, detail);
            AddText(history, "actor", actor, 10);
            history.Parameters.Add("time", NpgsqlDbType.Timestamp).Value = startTime;
            history.ExecuteNonQuery();
        }
    }

    private static void AddDetailParameters(NpgsqlCommand command, string factorSerial, ChartDetail detail)
    {
        AddText(command, "serial", factorSerial, 50); AddNumeric(command, "spcid", detail.SpcId);
        AddText(command, "us", detail.Us, 15); AddText(command, "ls", detail.Ls, 15);
        AddText(command, "ucl", detail.Ucl, 15); AddText(command, "lcl", detail.Lcl, 15); AddText(command, "cl", detail.Cl, 15);
        AddText(command, "target", detail.Target, 10); AddText(command, "cpk", detail.CpkGoal, 10);
        AddText(command, "from", detail.YAxisFrom, 10); AddText(command, "to", detail.YAxisTo, 10); AddText(command, "cs", detail.Cs, 15);
    }

    private static void InsertChartRules(NpgsqlConnection connection, NpgsqlTransaction transaction, string factorSerial, IReadOnlyList<ChartRule> rules)
    {
        foreach (var rule in rules) InsertChartRule(connection, transaction, factorSerial, rule);
    }

    private static void ReplaceProvidedRules(NpgsqlConnection connection, NpgsqlTransaction transaction, string factorSerial, Dictionary<int, List<ChartRule>> rules)
    {
        foreach (var (spcId, items) in rules)
        {
            using var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblspcqcchartrule WHERE factorserial=@serial AND spcid=@spcid;", connection, transaction);
            AddText(delete, "serial", factorSerial, 50); AddNumeric(delete, "spcid", spcId); delete.ExecuteNonQuery();
            InsertChartRules(connection, transaction, factorSerial, items);
        }
    }

    private static void InsertChartRule(NpgsqlConnection connection, NpgsqlTransaction transaction, string factorSerial, ChartRule rule)
    {
        using var command = new NpgsqlCommand("""
            INSERT INTO "YMES".tblspcqcchartrule(factorserial,spcid,ruleno,action,equipmentstate,erfstate,emailstate)
            VALUES(@serial,@spcid,@rule,@action,@equipment,@erf,@email);
            """, connection, transaction);
        AddText(command, "serial", factorSerial, 50); AddNumeric(command, "spcid", rule.SpcId);
        AddText(command, "rule", rule.RuleNo, 30); AddNumeric(command, "action", rule.Action);
        AddNumeric(command, "equipment", rule.EquipmentState); AddNumeric(command, "erf", rule.ErfState); AddNumeric(command, "email", rule.EmailState);
        command.ExecuteNonQuery();
    }

    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table, string displayName)
    {
        if (!TableExists(connection, transaction, table))
            throw new Qc302RuleException(StatusCodes.Status503ServiceUnavailable, $"YMES 尚未建立 {displayName}；此列未寫入。 ");
    }

    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT to_regclass(@name) IS NOT NULL;", connection, transaction);
        command.Parameters.AddWithValue("name", $"\"YMES\".{table}");
        return (bool)command.ExecuteScalar()!;
    }

    private static void ValidateWorkbook(IFormFile? file)
    {
        if (file is null || file.Length == 0) throw new Qc302RuleException(400, "請選擇要預覽的 Excel 工作簿。 ");
        if (file.Length > MaxWorkbookBytes) throw new Qc302RuleException(413, "Excel 工作簿不可超過 20 MB。 ");
    }

    private static Dictionary<string, string> NormalizeRequestRow(Dictionary<string, JsonElement>? input)
    {
        if (input is null) throw new Qc302RuleException(400, "匯入列不可為 NULL；該列未寫入。 ");
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in input)
        {
            var key = name.Trim();
            if (key.Length == 0 || key.Length > MaxHeaderLength) throw new Qc302RuleException(400, "匯入欄名不可空白或過長。 ");
            if (IsSystemColumn(key)) continue;
            var text = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? "",
                JsonValueKind.Number => value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null or JsonValueKind.Undefined => "",
                _ => throw new Qc302RuleException(400, $"欄位 {key} 必須是純量值。 ")
            };
            if (!result.TryAdd(key, text.Trim())) throw new Qc302RuleException(400, $"欄名重複：{key}。 ");
        }
        return result;
    }

    private static string RequiredText(Dictionary<string, string> row, string field, int maxLength)
    {
        var value = Get(row, field).Trim();
        if (value.Length == 0 || value.Length > maxLength || value.Any(char.IsControl))
            throw new Qc302RuleException(400, $"{field} 為必填且不可超過 {maxLength} 個字元。 ");
        return value;
    }

    private static string Get(Dictionary<string, string> row, string field) => row.TryGetValue(field, out var value) ? value.Trim() : "";
    private static bool IsSystemColumn(string name) => SystemColumns.Contains(name, StringComparer.OrdinalIgnoreCase);
    private static string CellText(object? value) => value switch
    {
        null or DBNull => "",
        DateTime date => date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        double number => number.ToString("G17", CultureInfo.InvariantCulture),
        float number => number.ToString("G9", CultureInfo.InvariantCulture),
        decimal number => number.ToString(CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? ""
    };
    private static string ChartName(int id) => id switch { 0 => "趨勢圖", 1 => "Xbar 圖", 2 => "R 圖", 3 => "S 圖", _ => "Rm 圖" };
    private static int? NullableInt(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    private static void AddText(NpgsqlCommand command, string name, string? value, int maxLength)
    {
        if (value is { Length: > 0 } && value.Length > maxLength) throw new Qc302RuleException(400, $"{name} 超過欄位長度。 ");
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;
    }
    private static void AddNumeric(NpgsqlCommand command, string name, int? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Integer).Value = (object?)value ?? DBNull.Value;

    private sealed record QcItem(string QcItemNo, int? SampleSize, int SpecLineType, int? ComponentCollect);
    private sealed record OnlineFactor(string FactorNo, string FactorSpec);
    private sealed record ChartMap(int SpcId, string Prefix, bool Trend);
    private sealed record ChartDetail(int SpcId, string Us, string Ls, string Ucl, string Lcl, string Cl,
        string Target, string CpkGoal, string YAxisFrom, string YAxisTo, string Cs);
    private sealed record ChartRule(int SpcId, string RuleNo, int Action, int? EquipmentState, int? ErfState, int? EmailState);
}
