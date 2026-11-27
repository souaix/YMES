using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc215Repository(NpgsqlDataSource dataSource)
{
    private const long MaxWorkbookBytes = 5L * 1024 * 1024;
    private const int MaxRows = 1000;
    private const int MaxColumns = 128;
    private const int MaxHeaderLength = 128;
    private static readonly HashSet<string> ImportColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "QCItemNo", "QCItemName", "LotIsVaild", "PlanNo", "ComponentCollect", "RuleNo", "Description",
        "LimitLotNum", "MClassNo", "FactorNo", "FactorOnline", "InlineOP", "OfflineOP",
        "INSPTYPE_FAI", "INSPTYPE_IPQC", "INSPTYPE_PQC", "Reason", "Creator", "CreateDate"
    };
    private static readonly HashSet<string> ServerOnlyColumns = new(StringComparer.OrdinalIgnoreCase)
        { "Creator", "CreateDate", "Reviser", "ReviseDate", "DataStamp", "IssueState", "Select", "Result", "Execute" };
    private static readonly string[] RequiredTables =
    [
        "tblqcitemforattributes", "tblqcitemforvariables", "tblqcitemparameter", "tblqcitemfactor",
        "tblqcitemreasonmap", "tblqcsampleplanchange", "tblopqcitem", "tblwiptemp_qcitem", "tblspcattbasislog",
        "tblmtlqcitem", "tblmtltypeqcitem", "tblopgqcitemdetail", "tblprdqcitem", "tblqcitemattgorderdetail",
        "tblqcitemgroupdetail", "tblqcspecbasis", "tblspcqcchartbasis", "tblqcitemrule", "tblqccalculatescript",
        "tblspcattlimit", "tblqcattrulebasis", "tblqcmclassbasis",
        "tblsysmesparameter", "tblopbasis", "tblqcreasonbasis", "tblqcitemgroupbasis", "tblqcitemgroupbasis_pmi",
        "tblqcitemattgrouporderbasis"
    ];
    private static readonly string[] ChildTables =
        ["tblqcitemparameter", "tblqcitemfactor", "tblqcitemreasonmap", "tblqcsampleplanchange"];

    public Qc215PreviewResponse Preview(IFormFile? file)
    {
        if (file is null || file.Length == 0) throw Rule(400, "請選擇 .xls 或 .xlsx 匯入檔案。");
        if (file.Length > MaxWorkbookBytes) throw Rule(413, "QC215 匯入檔不可超過 5 MiB。");
        var extension = Path.GetExtension(file.FileName);
        if (!extension.Equals(".xls", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw Rule(415, "正式 QC215 匯入只接受 .xls 或 .xlsx。");

        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var stream = file.OpenReadStream();
            using var workbook = ExcelReaderFactory.CreateReader(stream);
            if (!workbook.Read() || workbook.FieldCount < 1) throw Rule(400, "Excel 第一個工作表沒有欄名列。");
            if (workbook.FieldCount > MaxColumns) throw Rule(400, $"Excel 欄數不可超過 {MaxColumns} 欄。");
            var headers = Enumerable.Range(0, workbook.FieldCount)
                .Select(index => CellText(workbook.GetValue(index)))
                .Select((name, index) => name.Length == 0 ? $"unknow{index}" : name.Trim()).ToArray();
            if (headers.Any(name => name.Length > MaxHeaderLength)) throw Rule(400, $"Excel 欄名不可超過 {MaxHeaderLength} 個字元。");
            var duplicate = headers.Where(name => ImportColumns.Contains(name) && !ServerOnlyColumns.Contains(name))
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null) throw Rule(400, $"Excel 欄名重複：{duplicate.Key}。");
            var columns = headers.Where(name => ImportColumns.Contains(name) && !ServerOnlyColumns.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            // The MES workbook reserves row 2 for sample/explanation values.
            _ = workbook.Read();
            var rows = new List<IReadOnlyDictionary<string, string>>();
            while (workbook.Read())
            {
                var values = Enumerable.Range(0, headers.Length)
                    .Select(index => index < workbook.FieldCount ? CellText(workbook.GetValue(index)) : "").ToArray();
                if (values.All(string.IsNullOrWhiteSpace)) break;
                if (rows.Count >= MaxRows) throw Rule(400, $"單一工作簿最多預覽 {MaxRows} 筆資料。");
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < headers.Length; index++)
                    if (ImportColumns.Contains(headers[index]) && !ServerOnlyColumns.Contains(headers[index]))
                        row.Add(headers[index], values[index].Trim());
                rows.Add(row);
            }
            return new(columns, rows);
        }
        catch (Qc215RuleException) { throw; }
        catch (Exception ex) when (ex is InvalidDataException or IOException or ArgumentException or FormatException or NotSupportedException or System.Xml.XmlException
                                   || ex.GetType().Namespace == "ExcelDataReader.Exceptions")
        {
            throw Rule(400, "無法讀取 Excel；請確認檔案有效、未加密且第一張工作表格式正確。");
        }
    }

    public IReadOnlyList<Qc215ImportRowResult> Process(Qc215ImportRequest request, string actor, bool save)
    {
        if (request.Rows is null) throw Rule(400, "rows 不可為 NULL。");
        if (request.Rows.Count > MaxRows) throw Rule(400, $"單次最多處理 {MaxRows} 列。");
        if (request.Rows.Any(row => row is null)) throw Rule(400, "匯入資料不可含 NULL 列。");
        actor = Code(actor, "登入帳號", 10, true);
        var normalized = request.Rows.Select(NormalizeRequestRow).ToArray();
        var duplicateRows = normalized.Select((row, index) => (No: Get(row, "QCItemNo").Trim(), RowNo: index + 1))
            .Where(value => value.No.Length > 0).GroupBy(value => value.No, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).SelectMany(group => group.Select(value => value.RowNo)).ToHashSet();
        var results = new List<Qc215ImportRowResult>(normalized.Length);
        for (var index = 0; index < normalized.Length; index++)
        {
            var rowNo = index + 1;
            var itemNo = Get(normalized[index], "QCItemNo").Trim();
            if (duplicateRows.Contains(rowNo))
            {
                results.Add(Failure(rowNo, itemNo, "本批次 QCItemNo 重複；請先保留一列再處理。"));
                continue;
            }
            try
            {
                ProcessOne(normalized[index], actor, save);
                results.Add(new(rowNo, itemNo, true, 0, save ? "success" : ""));
            }
            catch (Qc215RuleException ex) { results.Add(Failure(rowNo, itemNo, ex.Message)); }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            { results.Add(Failure(rowNo, itemNo, "QCItemNo 或關聯鍵已存在；該列交易已回復。")); }
            catch (PostgresException)
            { results.Add(Failure(rowNo, itemNo, "資料庫操作失敗；該列交易已回復，請確認 YMES 權限與相依表。")); }
        }
        return results;
    }

    private void ProcessOne(Dictionary<string, string> row, string actor, bool save)
    {
        var value = ParseAndValidateFields(row);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            EnsureTables(connection, transaction, RequiredTables);
            EnsureTable(connection, transaction, value.ComponentCollect == 1 ? "tblqcsamplingplan_wafer" : "tblqcsamplingplan");
            if (value.ComponentCollect == 0) EnsureTable(connection, transaction, "tblqcsamplingparameter");

            QcItemIdentityGuard.Acquire(connection, transaction, [value.QcItemNo]);
            QcItemIdentityGuard.RequireTables(connection, transaction);
            QcItemIdentityGuard.EnsureAttributeOwner(connection, transaction, value.QcItemNo);
            EnsureNoGroupIdentityCollision(connection, transaction, value.QcItemNo);
            var current = ReadCurrentItem(connection, transaction, value.QcItemNo, save);
            if (current is { IssueState: 1 }) throw Rule(409, "QCItemNo 正在簽核中，不能由匯入覆寫。");
            if (current is not null) EnsureChildOwners(connection, transaction, current.QcItemNo);

            var plan = ValidateLookups(connection, transaction, value, lockRows: save);
            if (current is not null) EnsureNoExternalReferences(connection, transaction, current.QcItemNo, lockRows: save);
            if (!save) { transaction.Commit(); return; }

            if (current is not null) DeleteExistingItem(connection, transaction, current.QcItemNo);
            InsertItem(connection, transaction, value, actor);
            InsertParameters(connection, transaction, value, plan);
            InsertFactors(connection, transaction, value);
            InsertOperations(connection, transaction, value, actor);
            InsertReasons(connection, transaction, value, actor);
            transaction.Commit();
        }
        catch
        {
            try { transaction.Rollback(); } catch { /* retain original failure */ }
            throw;
        }
    }

    private static ImportItem ParseAndValidateFields(Dictionary<string, string> row)
    {
        var itemNo = Code(Get(row, "QCItemNo"), "QCItemNo", 25, true).ToUpperInvariant();
        var name = TextValue(Get(row, "QCItemName"), "QCItemName", 500, true);
        var lotIsValid = RequiredInt(row, "LotIsVaild");
        if (lotIsValid is not (0 or 1)) throw Rule(422, "LotIsVaild 必須為 0 或 1。");
        var componentCollect = OptionalInt(row, "ComponentCollect", 0);
        if (componentCollect is not (0 or 1)) throw Rule(422, "ComponentCollect 必須為 0 或 1。");
        if (componentCollect == 1 && lotIsValid != 1) throw Rule(422, "ComponentCollect=1 時 LotIsVaild 必須為 1。");
        var planNo = Code(Get(row, "PlanNo"), "PlanNo", 25, true).ToUpperInvariant();
        var ruleNo = Code(Get(row, "RuleNo"), "RuleNo", 20, false)?.ToUpperInvariant();
        var description = TextValue(Get(row, "Description"), "Description", 255, false);
        var limitLotNum = RequiredInt(row, "LimitLotNum");
        if (limitLotNum is < 1 or > 99999) throw Rule(422, "LimitLotNum 必須為 1 至 99999 的正整數。");
        var mClassNo = Code(Get(row, "MClassNo"), "MClassNo", 30, false)?.ToUpperInvariant();
        var factorNos = SplitCodes(Get(row, "FactorNo"), "FactorNo", 30, 100);
        var factorOnline = SplitCodes(Get(row, "FactorOnline"), "FactorOnline", 30, 100);
        if ((factorNos.Count == 0) != (factorOnline.Count == 0))
            throw Rule(422, "FactorNo 與 FactorOnline 必須同時填寫或同時空白。");
        if (factorOnline.Except(factorNos, StringComparer.OrdinalIgnoreCase).Any())
            throw Rule(422, "FactorOnline 項目必須包含於 FactorNo。");
        var inlineOps = SplitCodes(Get(row, "InlineOP"), "InlineOP", 20, 100);
        var offlineOps = SplitCodes(Get(row, "OfflineOP"), "OfflineOP", 20, 100);
        if (inlineOps.Count > 0 && lotIsValid != 1) throw Rule(422, "有 InlineOP 時 LotIsVaild 必須為 1。");
        if (inlineOps.Intersect(offlineOps, StringComparer.OrdinalIgnoreCase).Any())
            throw Rule(422, "同一作業站不可同時列入 InlineOP 與 OfflineOP。");
        if (inlineOps.Count + offlineOps.Count > 100) throw Rule(422, "InlineOP 與 OfflineOP 合計最多 100 個作業站。");
        var inspFai = OptionalFlag(row, "INSPTYPE_FAI", 0);
        var inspIpqc = OptionalFlag(row, "INSPTYPE_IPQC", 0);
        var inspPqc = OptionalFlag(row, "INSPTYPE_PQC", 1);
        var reasons = SplitCodes(Get(row, "Reason"), "Reason", 20, 100);
        return new(itemNo, name, lotIsValid, planNo, componentCollect, ruleNo, description, limitLotNum,
            mClassNo, factorNos, factorOnline, inlineOps, offlineOps, inspFai, inspIpqc, inspPqc, reasons);
    }

    private static SamplingPlan ValidateLookups(NpgsqlConnection connection, NpgsqlTransaction transaction, ImportItem item, bool lockRows)
    {
        SamplingPlan plan;
        if (item.ComponentCollect == 0)
        {
            var general = QcSamplingPlanReference.ReadGeneral(connection, transaction, item.PlanNo, 0, 2, Rule);
            if (general.Parameters.Count == 0) throw Rule(422, $"一般抽樣計畫 {general.PlanNo} 沒有抽樣參數。");
            plan = new(general.PlanNo, general.Parameters.Select(parameter => new PlanParameter(parameter.ParameterNo, parameter.ParameterValue)).ToArray());
        }
        else
        {
            using var command = new NpgsqlCommand("SELECT planno,issuestate::int FROM \"YMES\".tblqcsamplingplan_wafer WHERE lower(btrim(planno))=lower(btrim(@no)) ORDER BY planno" + (lockRows ? " FOR SHARE;" : ";"), connection, transaction);
            Text(command, "no", item.PlanNo, 25);
            using var reader = command.ExecuteReader();
            if (!reader.Read()) throw Rule(422, $"找不到已核准的 Wafer 抽樣計畫 {item.PlanNo}。");
            var canonical = reader.GetString(0); var state = reader.GetInt32(1);
            if (reader.Read()) throw Rule(409, "Wafer PlanNo 存在大小寫／空白歧義，不能匯入。");
            if (state != 2) throw Rule(422, $"Wafer 抽樣計畫 {canonical} 尚未核准。");
            plan = new(canonical, []);
        }
        item.PlanNo = plan.PlanNo;
        if (!string.IsNullOrWhiteSpace(item.RuleNo))
        {
            using var command = new NpgsqlCommand("SELECT ruleno,componentcollect::int,issuestate::int FROM \"YMES\".tblqcattrulebasis WHERE lower(btrim(ruleno))=lower(btrim(@no)) ORDER BY ruleno" + (lockRows ? " FOR SHARE;" : ";"), connection, transaction);
            Text(command, "no", item.RuleNo, 20);
            using var reader = command.ExecuteReader();
            if (!reader.Read()) throw Rule(422, $"計數型品管法則 {item.RuleNo} 不存在或尚未核准。");
            var canonical = reader.GetString(0); var component = reader.GetInt32(1); var state = reader.GetInt32(2);
            if (reader.Read()) throw Rule(409, "RuleNo 存在大小寫／空白歧義。");
            if (state != 2) throw Rule(422, $"品管法則 {canonical} 尚未核准。");
            if (component != item.ComponentCollect) throw Rule(422, "RuleNo 的 ComponentCollect 與匯入資料不符。");
            item.RuleNo = canonical;
        }
        if (!string.IsNullOrWhiteSpace(item.MClassNo)) item.MClassNo = ApprovedCode(connection, transaction, "tblqcmclassbasis", "mclassno", item.MClassNo, 30, lockRows);

        var catalog = new List<ParameterCatalog>();
        using (var command = new NpgsqlCommand("SELECT parameterno,tablename,fieldname FROM \"YMES\".tblsysmesparameter WHERE parameterclass=1 AND issuestate=2 ORDER BY lower(btrim(parameterno)),parameterno" + (lockRows ? " FOR SHARE;" : ";"), connection, transaction))
        using (var reader = command.ExecuteReader())
            while (reader.Read()) catalog.Add(new(reader.GetString(0), NullableText(reader, 1), NullableText(reader, 2)));
        if (catalog.Count == 0) throw Rule(422, "尚無已核准的 ParameterClass=1 MES 要因目錄；QC215 不可將其當成空白選項。");
        EnsureUnique(catalog.Select(parameter => parameter.ParameterNo), "MES ParameterNo", stored: true);
        foreach (var requested in item.FactorNos)
        {
            var match = catalog.SingleOrDefault(parameter => Same(parameter.ParameterNo, requested));
            if (match is null) throw Rule(422, $"FactorNo {requested} 不存在於已核准 ParameterClass=1 要因目錄。");
            if (match.TableName?.Length > 60 || match.FieldName?.Length > 30)
                throw Rule(422, $"FactorNo {requested} 的 MES 欄位 metadata 超出 YMES Factor 欄位長度。");
            item.CanonicalFactors[requested] = match;
        }
        foreach (var requested in item.InlineOps.Concat(item.OfflineOps))
        {
            using var command = new NpgsqlCommand("SELECT opno,issuestate::int FROM \"YMES\".tblopbasis WHERE lower(btrim(opno))=lower(btrim(@no)) ORDER BY opno" + (lockRows ? " FOR SHARE;" : ";"), connection, transaction);
            Text(command, "no", requested, 20);
            using var reader = command.ExecuteReader();
            if (!reader.Read()) throw Rule(422, $"作業站 {requested} 不存在或尚未核准。");
            var canonical = reader.GetString(0); var state = reader.GetInt32(1);
            if (reader.Read()) throw Rule(409, $"作業站 {requested} 有大小寫／空白重複主檔。");
            if (state != 2) throw Rule(422, $"作業站 {canonical} 尚未核准。");
            item.CanonicalOperations[requested] = canonical;
        }
        foreach (var requested in item.Reasons)
            item.CanonicalReasons[requested] = ApprovedCode(connection, transaction, "tblqcreasonbasis", "reasonno", requested, 20, lockRows);
        return plan;
    }

    private static string ApprovedCode(NpgsqlConnection connection, NpgsqlTransaction transaction, string table, string column, string value, int maxLength, bool lockRow)
    {
        // table and column are compile-time allowlisted constants at every call site.
        using var command = new NpgsqlCommand($"SELECT {column},issuestate::int FROM \"YMES\".{table} WHERE lower(btrim({column}))=lower(btrim(@value)) ORDER BY {column}" + (lockRow ? " FOR SHARE;" : ";"), connection, transaction);
        Text(command, "value", value, maxLength);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(422, $"{value} 不存在或尚未核准。");
        var canonical = reader.GetString(0); var state = reader.GetInt32(1);
        if (reader.Read()) throw Rule(409, $"{value} 存在大小寫／空白歧義。");
        if (state != 2) throw Rule(422, $"{canonical} 尚未核准。");
        return canonical;
    }

    private static void EnsureNoGroupIdentityCollision(NpgsqlConnection connection, NpgsqlTransaction transaction, string itemNo)
    {
        using var command = new NpgsqlCommand("""
            SELECT EXISTS(
                SELECT 1 FROM "YMES".tblqcitemgroupbasis WHERE lower(btrim(groupno))=lower(btrim(@no))
                UNION ALL SELECT 1 FROM "YMES".tblqcitemgroupbasis_pmi WHERE lower(btrim(groupno))=lower(btrim(@no))
                UNION ALL SELECT 1 FROM "YMES".tblqcitemattgrouporderbasis WHERE lower(btrim(groupno))=lower(btrim(@no)));
            """, connection, transaction);
        Text(command, "no", itemNo, 25);
        if (command.ExecuteScalar() is true) throw Rule(409, "QCItemNo 與現有計量／計數／PMI 群組編號相同；未寫入。");
    }

    private static ExistingItem? ReadCurrentItem(NpgsqlConnection connection, NpgsqlTransaction transaction, string itemNo, bool lockRow)
    {
        using var command = new NpgsqlCommand("SELECT qcitemno,issuestate::int FROM \"YMES\".tblqcitemforattributes WHERE lower(btrim(qcitemno))=lower(btrim(@no)) ORDER BY qcitemno" + (lockRow ? " FOR UPDATE;" : ";"), connection, transaction);
        Text(command, "no", itemNo, 25);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var result = new ExistingItem(reader.GetString(0), reader.GetInt32(1));
        if (reader.Read()) throw Rule(409, "計數 QCItemNo 存在大小寫／空白別名，無法安全覆寫。");
        return result;
    }

    private static void EnsureChildOwners(NpgsqlConnection connection, NpgsqlTransaction transaction, string itemNo)
    {
        foreach (var table in ChildTables)
        {
            using var command = new NpgsqlCommand($"SELECT 1 FROM \"YMES\".{table} WHERE lower(btrim(qcitemno))=lower(btrim(@no)) AND qcitemno<>@no LIMIT 1;", connection, transaction);
            Text(command, "no", itemNo, 25);
            if (command.ExecuteScalar() is not null) throw Rule(409, "既有 QC Item 明細含大小寫／空白別名，無法安全判定整組資料；本次未修改。");
        }
    }

    private static void EnsureNoExternalReferences(NpgsqlConnection connection, NpgsqlTransaction transaction, string itemNo, bool lockRows)
    {
        var checks = new (string Table, string Sql)[]
        {
            ("tblspcattbasislog", "SELECT 1 FROM \"YMES\".tblspcattbasislog WHERE lower(btrim(qcitemno))=lower(btrim(@no)) LIMIT 1"),
            ("tblwiptemp_qcitem", "SELECT 1 FROM \"YMES\".tblwiptemp_qcitem WHERE lower(btrim(qcitemno))=lower(btrim(@no)) AND qcitemtype=0 LIMIT 1"),
            ("tblmtlqcitem", "SELECT 1 FROM \"YMES\".tblmtlqcitem WHERE lower(btrim(qcitemno))=lower(btrim(@no)) AND qcitemtype=0 LIMIT 1"),
            ("tblmtltypeqcitem", "SELECT 1 FROM \"YMES\".tblmtltypeqcitem WHERE lower(btrim(qcitemno))=lower(btrim(@no)) AND qcitemtype=0 LIMIT 1"),
            ("tblopqcitem", "SELECT 1 FROM \"YMES\".tblopqcitem WHERE lower(btrim(qcitemno))=lower(btrim(@no)) LIMIT 1"),
            ("tblopgqcitemdetail", "SELECT 1 FROM \"YMES\".tblopgqcitemdetail WHERE lower(btrim(qcitemno))=lower(btrim(@no)) AND qcitemtype=0 LIMIT 1"),
            ("tblprdqcitem", "SELECT 1 FROM \"YMES\".tblprdqcitem WHERE lower(btrim(qcitemno))=lower(btrim(@no)) AND qcitemtype=0 LIMIT 1"),
            ("tblqcitemattgorderdetail", "SELECT 1 FROM \"YMES\".tblqcitemattgorderdetail WHERE lower(btrim(qcitemno))=lower(btrim(@no)) LIMIT 1"),
            ("tblqcitemgroupdetail", "SELECT 1 FROM \"YMES\".tblqcitemgroupdetail WHERE lower(btrim(qcitemno))=lower(btrim(@no)) LIMIT 1"),
            ("tblqcspecbasis", "SELECT 1 FROM \"YMES\".tblqcspecbasis WHERE lower(btrim(qcitemno))=lower(btrim(@no)) LIMIT 1"),
            ("tblspcqcchartbasis", "SELECT 1 FROM \"YMES\".tblspcqcchartbasis WHERE lower(btrim(qcitemno))=lower(btrim(@no)) LIMIT 1")
        };
        foreach (var (table, sql) in checks)
        {
            EnsureTable(connection, transaction, table);
            using var command = new NpgsqlCommand(sql + (lockRows ? " FOR SHARE;" : ";"), connection, transaction);
            Text(command, "no", itemNo, 25);
            if (command.ExecuteScalar() is not null) throw Rule(409, $"QCItemNo 已在 {table} 有使用或量測資料，不能由匯入覆寫。");
        }
        foreach (var optionalLog in new[] { "tblspcattdetaillog", "tblspcattdetaillog_tape" })
        {
            if (!TableExists(connection, transaction, optionalLog)) continue;
            using var command = new NpgsqlCommand($"SELECT 1 FROM \"YMES\".{optionalLog} WHERE lower(btrim(qcitemno))=lower(btrim(@no)) LIMIT 1" + (lockRows ? " FOR SHARE;" : ";"), connection, transaction);
            Text(command, "no", itemNo, 25);
            if (command.ExecuteScalar() is not null) throw Rule(409, $"QCItemNo 已在 {optionalLog} 有量測資料，不能由匯入覆寫。");
        }
        if (TableExists(connection, transaction, "tblqcitemgroupdetail_pmi"))
        {
            using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblqcitemgroupdetail_pmi WHERE lower(btrim(qcitemno))=lower(btrim(@no)) LIMIT 1" + (lockRows ? " FOR SHARE;" : ";"), connection, transaction);
            Text(command, "no", itemNo, 25);
            if (command.ExecuteScalar() is not null) throw Rule(409, "QCItemNo 已綁定 PMI 群組，不能由匯入覆寫。");
        }
    }

    private static void DeleteExistingItem(NpgsqlConnection connection, NpgsqlTransaction transaction, string itemNo)
    {
        // These are source-defined child/configuration rows removed by the legacy import cleanup.
        foreach (var table in new[]
        {
            "tblqcitemparameter", "tblqcitemfactor", "tblqcitemreasonmap", "tblqcsampleplanchange",
            "tblqcitemrule", "tblqccalculatescript", "tblspcattlimit"
        })
        {
            using var command = new NpgsqlCommand($"DELETE FROM \"YMES\".{table} WHERE qcitemno=@no" +
                (table == "tblqccalculatescript" ? " AND groupno='N/A'" : "") + ";", connection, transaction);
            Text(command, "no", itemNo, 25); command.ExecuteNonQuery();
        }
        // Older SPC support tables are optional in YMES. Delete only from the fixed, source-defined
        // allowlist when deployed; never construct a table name from uploaded content.
        foreach (var table in new[]
        {
            "tblspcqcatrulemapdetail", "tblspcqcatrulemapparameter", "tblspcqcatrulemapbasis",
            "tblspclogforsplanchange", "tblqcitemprdreasonmap"
        })
        {
            if (!TableExists(connection, transaction, table)) continue;
            using var command = new NpgsqlCommand($"DELETE FROM \"YMES\".{table} WHERE qcitemno=@no;", connection, transaction);
            Text(command, "no", itemNo, 25); command.ExecuteNonQuery();
        }
        using var delete = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcitemforattributes WHERE qcitemno=@no;", connection, transaction);
        Text(delete, "no", itemNo, 25);
        if (delete.ExecuteNonQuery() != 1) throw Rule(409, "計數 QC Item 已變更，請重新檢查後再匯入。");
    }

    private static void InsertItem(NpgsqlConnection connection, NpgsqlTransaction transaction, ImportItem value, string actor)
    {
        using var command = new NpgsqlCommand("""
            INSERT INTO "YMES".tblqcitemforattributes
                (qcitemno,qcitemname,lotisvalid,planno,ruleno,formname,description,issuestate,creator,createdate,
                 samplesize,limitlotnum,componentcollect,mclassno,datastamp)
            VALUES(@no,@name,@lot,@plan,NULLIF(@rule,''),'',@description,0,@actor,now() AT TIME ZONE 'UTC',
                   0,@limit,@component,NULLIF(@mclass,''),0);
            """, connection, transaction);
        Text(command, "no", value.QcItemNo, 25); Text(command, "name", value.QcItemName, 500);
        Number(command, "lot", value.LotIsValid); Text(command, "plan", value.PlanNo, 25);
        Text(command, "rule", value.RuleNo, 20); Text(command, "description", value.Description, 255);
        Text(command, "actor", actor, 10); Number(command, "limit", value.LimitLotNum);
        Number(command, "component", value.ComponentCollect); Text(command, "mclass", value.MClassNo, 30);
        command.ExecuteNonQuery();
    }

    private static void InsertParameters(NpgsqlConnection connection, NpgsqlTransaction transaction, ImportItem value, SamplingPlan plan)
    {
        if (value.ComponentCollect != 0) return;
        foreach (var parameter in plan.Parameters)
        {
            using var command = new NpgsqlCommand("INSERT INTO \"YMES\".tblqcitemparameter(qcitemno,parameterno,parametervalue) VALUES(@no,@parameter,@value);", connection, transaction);
            Text(command, "no", value.QcItemNo, 25); Text(command, "parameter", parameter.ParameterNo, 20);
            Text(command, "value", parameter.ParameterValue, 255); command.ExecuteNonQuery();
        }
    }

    private static void InsertFactors(NpgsqlConnection connection, NpgsqlTransaction transaction, ImportItem value)
    {
        var online = new HashSet<string>(value.FactorOnline, StringComparer.OrdinalIgnoreCase);
        foreach (var factorNo in value.FactorNos)
        {
            var catalog = value.CanonicalFactors[factorNo];
            // QC215 has no FactorSpec workbook column; preserve source behavior with the non-spec default.
            using var command = new NpgsqlCommand("""
                INSERT INTO "YMES".tblqcitemfactor(qcitemno,factorno,factoronline,factorspec,opno,tablename,fieldname,factorshortrun)
                VALUES(@no,@factor,@online,'N',NULL,@table,@field,NULL);
                """, connection, transaction);
            Text(command, "no", value.QcItemNo, 25); Text(command, "factor", catalog.ParameterNo, 30);
            Text(command, "online", online.Contains(factorNo) ? "Y" : "N", 1);
            Text(command, "table", catalog.TableName, 60); Text(command, "field", catalog.FieldName, 30);
            command.ExecuteNonQuery();
        }
    }

    private static void InsertOperations(NpgsqlConnection connection, NpgsqlTransaction transaction, ImportItem value, string actor)
    {
        var order = 0;
        foreach (var (operationNos, inline) in new[] { (value.InlineOps, true), (value.OfflineOps, false) })
        foreach (var requested in operationNos)
        {
            using var command = new NpgsqlCommand("""
                INSERT INTO "YMES".tblopqcitem
                    (opno,qcitemno,qcitemtype,inline,executeorder,insptype_fai,insptype_ipqc,insptype_pqc,reviser,revisedate)
                VALUES(@op,@no,0,@inline,@order,@fai,@ipqc,@pqc,@actor,now() AT TIME ZONE 'UTC');
                """, connection, transaction);
            Text(command, "op", value.CanonicalOperations[requested], 20); Text(command, "no", value.QcItemNo, 25);
            Number(command, "inline", inline ? 1 : 0); Number(command, "order", order++);
            Number(command, "fai", inline ? value.InspTypeFai : 0);
            Number(command, "ipqc", inline ? value.InspTypeIpqc : 0);
            Number(command, "pqc", inline ? value.InspTypePqc : 0);
            Text(command, "actor", actor, 10); command.ExecuteNonQuery();
        }
    }

    private static void InsertReasons(NpgsqlConnection connection, NpgsqlTransaction transaction, ImportItem value, string actor)
    {
        foreach (var requested in value.Reasons)
        {
            using var command = new NpgsqlCommand("INSERT INTO \"YMES\".tblqcitemreasonmap(qcitemno,reasonno,creator,createdate,reviser,revisedate) VALUES(@no,@reason,@actor,now() AT TIME ZONE 'UTC',NULL,NULL);", connection, transaction);
            Text(command, "no", value.QcItemNo, 25); Text(command, "reason", value.CanonicalReasons[requested], 20);
            Text(command, "actor", actor, 10); command.ExecuteNonQuery();
        }
    }

    private static Dictionary<string, string> NormalizeRequestRow(Dictionary<string, JsonElement> input)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (rawName, value) in input)
        {
            var name = rawName.Trim();
            if (name.Length == 0 || name.Length > MaxHeaderLength) throw Rule(400, "匯入欄名不可空白或過長。");
            if (ServerOnlyColumns.Contains(name)) continue;
            if (!ImportColumns.Contains(name)) throw Rule(400, $"不接受 QC215 欄位 {name}；請依正式匯入欄位重新載入。");
            var text = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? "",
                JsonValueKind.Number => value.GetRawText(),
                JsonValueKind.True => "1",
                JsonValueKind.False => "0",
                JsonValueKind.Null or JsonValueKind.Undefined => "",
                _ => throw Rule(400, $"欄位 {name} 必須為純量文字或數值。")
            };
            if (!result.TryAdd(name, text.Trim())) throw Rule(400, $"匯入欄位重複：{name}。");
        }
        return result;
    }

    private static int RequiredInt(Dictionary<string, string> row, string field)
    {
        var text = Get(row, field);
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) throw Rule(422, $"{field} 必須為整數。");
        return value;
    }

    private static int OptionalInt(Dictionary<string, string> row, string field, int fallback)
    {
        var text = Get(row, field);
        return text.Length == 0 ? fallback : int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value : throw Rule(422, $"{field} 必須為整數。");
    }

    private static int OptionalFlag(Dictionary<string, string> row, string field, int fallback)
    {
        var value = OptionalInt(row, field, fallback);
        if (value is not (0 or 1)) throw Rule(422, $"{field} 必須為 0 或 1。");
        return value;
    }

    private static string Code(string value, string field, int maxLength, bool required)
    {
        var text = value.Trim();
        if ((required && text.Length == 0) || text.Length > maxLength || text.Any(char.IsControl))
            throw Rule(422, $"{field} 為必填且不可超過 {maxLength} 個字元或包含控制字元。");
        return text;
    }

    private static string TextValue(string value, string field, int maxLength, bool required)
    {
        if ((required && string.IsNullOrWhiteSpace(value)) || value.Length > maxLength ||
            value.Any(character => char.IsControl(character) && character is not '\r' and not '\n' and not '\t'))
            throw Rule(422, $"{field} 為必填且不可超過 {maxLength} 個字元或包含無效字元。");
        return value;
    }

    private static List<string> SplitCodes(string value, string field, int maxLength, int maxCount)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        var result = value.Split(';', StringSplitOptions.TrimEntries).Select(code => Code(code, field, maxLength, true)).ToList();
        if (result.Count > maxCount) throw Rule(422, $"{field} 最多 {maxCount} 筆。");
        EnsureUnique(result, field, stored: false);
        for (var index = 0; index < result.Count; index++) result[index] = result[index].ToUpperInvariant();
        return result;
    }

    private static void EnsureUnique(IEnumerable<string> values, string field, bool stored)
    {
        var duplicate = values.GroupBy(value => value.Trim(' '), StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null) throw Rule(stored ? 409 : 422, stored ? $"既有 {field} 存在大小寫／空白重複。" : $"{field} 不可重複：{duplicate.Key}。");
    }

    private static string Get(Dictionary<string, string> row, string field) => row.TryGetValue(field, out var value) ? value.Trim() : "";
    private static bool Same(string? left, string? right) => string.Equals(left?.Trim(' '), right?.Trim(' '), StringComparison.OrdinalIgnoreCase);
    private static string? NullableText(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static string CellText(object? value) => value switch
    {
        null or DBNull => "",
        DateTime date => date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        double number => number.ToString("G17", CultureInfo.InvariantCulture),
        float number => number.ToString("G9", CultureInfo.InvariantCulture),
        decimal number => number.ToString(CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? ""
    };

    private static void EnsureTables(NpgsqlConnection connection, NpgsqlTransaction transaction, IEnumerable<string> tables)
    {
        foreach (var table in tables) EnsureTable(connection, transaction, table);
    }

    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction transaction, string table)
    {
        if (!TableExists(connection, transaction, table)) throw Rule(503, $"YMES 尚未建立 QC215 必要相依表 {table}；本列未寫入。");
    }

    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@name AND c.relkind IN ('r','p'));", connection, transaction);
        command.Parameters.Add("name", NpgsqlDbType.Text).Value = table;
        return command.ExecuteScalar() is true;
    }

    private static void Text(NpgsqlCommand command, string name, string? value, int maxLength)
    {
        if (value is { Length: > 0 } && value.Length > maxLength) throw Rule(422, $"{name} 超出資料表欄位長度。");
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;
    }

    private static void Number(NpgsqlCommand command, string name, decimal? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;

    private static Qc215ImportRowResult Failure(int rowNo, string itemNo, string message) => new(rowNo, itemNo, false, 1, message);
    private static Qc215RuleException Rule(int status, string message) => new(status, message);

    private sealed record ExistingItem(string QcItemNo, int IssueState);
    private sealed record PlanParameter(string ParameterNo, string? ParameterValue);
    private sealed record SamplingPlan(string PlanNo, IReadOnlyList<PlanParameter> Parameters);
    private sealed record ParameterCatalog(string ParameterNo, string? TableName, string? FieldName);

    private sealed class ImportItem(string qcItemNo, string qcItemName, int lotIsValid, string planNo, int componentCollect,
        string? ruleNo, string description, int limitLotNum, string? mClassNo, List<string> factorNos,
        List<string> factorOnline, List<string> inlineOps, List<string> offlineOps, int inspTypeFai,
        int inspTypeIpqc, int inspTypePqc, List<string> reasons)
    {
        public string QcItemNo { get; } = qcItemNo;
        public string QcItemName { get; } = qcItemName;
        public int LotIsValid { get; } = lotIsValid;
        public string PlanNo { get; set; } = planNo;
        public int ComponentCollect { get; } = componentCollect;
        public string? RuleNo { get; set; } = ruleNo;
        public string Description { get; } = description;
        public int LimitLotNum { get; } = limitLotNum;
        public string? MClassNo { get; set; } = mClassNo;
        public List<string> FactorNos { get; } = factorNos;
        public List<string> FactorOnline { get; } = factorOnline;
        public List<string> InlineOps { get; } = inlineOps;
        public List<string> OfflineOps { get; } = offlineOps;
        public int InspTypeFai { get; } = inspTypeFai;
        public int InspTypeIpqc { get; } = inspTypeIpqc;
        public int InspTypePqc { get; } = inspTypePqc;
        public List<string> Reasons { get; } = reasons;
        public Dictionary<string, ParameterCatalog> CanonicalFactors { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> CanonicalOperations { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> CanonicalReasons { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
