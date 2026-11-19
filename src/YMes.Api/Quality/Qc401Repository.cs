using System.Globalization;
using System.Text;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc401Repository(NpgsqlDataSource dataSource)
{
    private const string Columns = "mclassno,description,creator,createdate,issuestate::int,datastamp,reviser,revisedate";
    private const int MaxImportRows = 10000;
    private const long MaxImportBytes = 5 * 1024 * 1024;
    private static readonly string[] ReferenceTables =
    [
        "tblqcsclassbasis", "tblqcholditembasis", "tblqcholddescription", "tblqcfirstgroup", "tblqcfinalgroup",
        "tblqcitemforattributes", "tblqcitemforvariables"
    ];

    public IReadOnlyList<Qc401MClassDto> List(string? searchFieldName, string? searchFieldValue)
    {
        if (!string.IsNullOrWhiteSpace(searchFieldName) && !string.Equals(searchFieldName.Trim(), "MClassNo", StringComparison.OrdinalIgnoreCase))
            throw Rule(400, "查詢欄位只允許 MClassNo。");
        var search = (searchFieldValue ?? "").Trim();
        if (search.Length > 30 || search.Any(char.IsControl)) throw Rule(400, "主分類查詢不可超過 30 個字元或含控制字元。");
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null);
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblqcmclassbasis WHERE @search='' OR position(lower(@search) in lower(mclassno))>0 ORDER BY mclassno;", connection);
        AddText(command, "search", search, 30);
        using var reader = command.ExecuteReader();
        var result = new List<Qc401MClassDto>();
        while (reader.Read()) result.Add(Read(reader));
        return result;
    }

    public Qc401MClassDto Get(string key)
    {
        var no = NormalizeKey(key);
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null);
        return Read(connection, null, no, false);
    }

    public Qc401MClassDto Create(Qc401MClassWriteRequest request, string actor)
    {
        actor = NormalizeActor(actor);
        var no = NormalizeKey(request.MClassNo);
        var description = NormalizeDescription(request.Description);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            using (var duplicate = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblqcmclassbasis WHERE lower(mclassno)=lower(@no);", connection, transaction))
            {
                AddText(duplicate, "no", no, 30);
                if (duplicate.ExecuteScalar() is not null) throw Rule(409, "主分類編號已存在。");
            }
            using (var command = new NpgsqlCommand("""
                INSERT INTO "YMES".tblqcmclassbasis(mclassno,description,creator,createdate,issuestate)
                VALUES(@no,@description,@actor,now(),0);
                """, connection, transaction))
            {
                AddText(command, "no", no, 30); AddText(command, "description", description, 255); AddText(command, "actor", actor, 10);
                command.ExecuteNonQuery();
            }
            var result = Read(connection, transaction, no, false);
            transaction.Commit();
            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Rollback(transaction); throw Rule(409, "主分類編號已存在。");
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc401MClassDto Update(string key, Qc401MClassWriteRequest request, int? issueState, decimal? stamp, string actor)
    {
        actor = NormalizeActor(actor);
        var no = NormalizeKey(key);
        if (!string.IsNullOrWhiteSpace(request.MClassNo) && !string.Equals(no, NormalizeKey(request.MClassNo), StringComparison.Ordinal))
            throw Rule(400, "主分類編號不可修改。");
        var description = NormalizeDescription(request.Description);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = Read(connection, transaction, no, true);
            EnsureExpected(current, issueState, stamp);
            if (current.IssueState is not (0 or 2)) throw Rule(409, "只有未簽核或已簽核的主分類資料可修改。");
            using var command = new NpgsqlCommand("""
                UPDATE "YMES".tblqcmclassbasis
                SET description=@description,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE mclassno=@no AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp;
                """, connection, transaction);
            AddText(command, "description", description, 255); BindExpected(command, current, actor);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
            var result = Read(connection, transaction, current.MClassNo, false);
            transaction.Commit(); return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc401MClassDto Delete(string key, int? issueState, decimal? stamp, string actor)
    {
        actor = NormalizeActor(actor);
        var no = NormalizeKey(key);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = Read(connection, transaction, no, true);
            EnsureExpected(current, issueState, stamp);
            if (current.IssueState == 1) throw Rule(409, "簽核中的主分類不可刪除。");
            if (current.IssueState == 2)
            {
                ChangeState(connection, transaction, current, actor, -1);
                var result = Read(connection, transaction, current.MClassNo, false);
                transaction.Commit(); return result;
            }
            if (current.IssueState is not (0 or -1)) throw Rule(409, "此主分類狀態不允許刪除。");
            EnsureNoReferences(connection, transaction, current.MClassNo);
            using var command = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcmclassbasis WHERE mclassno=@no AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction);
            BindExpected(command, current, actor);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
            transaction.Commit(); return current;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc401MClassDto Approve(string key, int? issueState, decimal? stamp, string actor)
    {
        actor = NormalizeActor(actor);
        var no = NormalizeKey(key);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction);
            var current = Read(connection, transaction, no, true);
            EnsureExpected(current, issueState, stamp);
            if (current.IssueState != 0) throw Rule(409, "只有未簽核的主分類資料可核准。");
            if (new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" }.Any(table => TableExists(connection, transaction, table)))
                throw Rule(503, "YMES 已存在 Issue/Job 流程表，但 QC401 尚未接入共用簽核服務；資料未變更。");
            ChangeState(connection, transaction, current, actor, 2);
            var result = Read(connection, transaction, current.MClassNo, false);
            transaction.Commit(); return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public IReadOnlyList<Qc401ImportPair> Preview(IFormFile? file)
    {
        if (file is null || file.Length == 0) throw Rule(400, "請選擇 .xls 或 .xlsx 檔案。");
        if (file.Length > MaxImportBytes) throw Rule(413, "主分類匯入檔不可超過 5 MiB。");
        var extension = Path.GetExtension(file.FileName);
        if (!extension.Equals(".xls", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw Rule(415, "正式主分類匯入只接受 .xls 或 .xlsx。");
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var stream = file.OpenReadStream();
            using var workbook = ExcelReaderFactory.CreateReader(stream);
            if (!workbook.Read() || workbook.FieldCount < 1) throw Rule(400, "Excel 第一個工作表沒有標題列。");
            var result = new List<Qc401ImportPair>();
            var seen = new HashSet<(string MClassNo, string Description)>();
            var rowNo = 1;
            while (workbook.Read())
            {
                if (++rowNo > MaxImportRows + 1) throw Rule(400, $"單次最多匯入 {MaxImportRows} 個資料列。");
                var no = CellText(workbook.GetValue(0)).Trim();
                var description = workbook.FieldCount > 1 ? CellText(workbook.GetValue(1)).Trim() : "";
                if (no.Length == 0 && description.Length == 0) continue;
                if (no.Length == 0) throw Rule(400, $"第 {rowNo} 列主分類編號必填。");
                // Class identity ignores case; Description remains exact text.
                // ABC and abc descriptions must remain separate and fail the
                // duplicate-class group check instead of silently losing a row.
                if (seen.Add((no.ToUpperInvariant(), description))) result.Add(new(no, description));
            }
            if (result.Count == 0) throw Rule(400, "Excel 第一個工作表沒有主分類資料列。");
            return result;
        }
        catch (Qc401RuleException) { throw; }
        catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or FormatException or NotSupportedException or System.Xml.XmlException
                                   || ex.GetType().Namespace == "ExcelDataReader.Exceptions")
        {
            throw Rule(400, "無法讀取 Excel，請確認檔案有效、未加密且第一個工作表可讀取。");
        }
    }

    public IReadOnlyList<Qc401ImportResult> Import(IFormFile? file, string actor, CancellationToken cancellationToken)
    {
        actor = NormalizeActor(actor);
        var pairs = Preview(file);
        var results = new List<Qc401ImportResult>();
        string? stopReason = null;
        foreach (var group in pairs.GroupBy(x => x.MClassNo.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var items = group.ToArray();
            if (cancellationToken.IsCancellationRequested) stopReason ??= "匯入已取消，本列尚未處理；先前成功資料已保留。";
            if (stopReason is { } reason)
            {
                results.AddRange(items.Select(x => new Qc401ImportResult(x.MClassNo, x.Description, "fail", reason)));
                continue;
            }
            if (items.Length > 1)
            {
                results.AddRange(items.Select(x => new Qc401ImportResult(x.MClassNo, x.Description, "fail", "同一 MClassNo 有多筆不同資料。")));
                continue;
            }
            var item = items[0];
            try
            {
                var row = Create(new Qc401MClassWriteRequest { MClassNo = item.MClassNo, Description = item.Description }, actor);
                results.Add(new(row.MClassNo, item.Description, "success", ""));
            }
            catch (Qc401RuleException ex) when (ex.StatusCode is >= 400 and < 500)
            {
                results.Add(new(item.MClassNo, item.Description, "fail", ex.Message));
            }
            catch (Qc401RuleException ex) when (ex.StatusCode >= 500)
            {
                results.Add(new(item.MClassNo, item.Description, "fail", ex.Message));
                stopReason = "資料服務中斷，本列尚未處理；先前成功資料已保留，請排除問題後再匯入。";
            }
            catch (NpgsqlException)
            {
                // Each earlier group already committed. Preserve those results;
                // a connection failure during commit leaves this row's outcome
                // uncertain, so require a requery rather than claiming rollback.
                results.Add(new(item.MClassNo, item.Description, "fail", "此筆資料庫操作中斷，結果請重新查詢確認；後續列尚未處理，先前成功資料已保留。"));
                stopReason = "資料服務中斷，本列尚未處理；先前成功資料已保留，請排除問題後再匯入。";
            }
        }
        return results;
    }

    private static string CellText(object? value) => value switch
    {
        null or DBNull => "",
        DateTime time => time.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture),
        IFormattable formatted => formatted.ToString(null, CultureInfo.InvariantCulture) ?? "",
        _ => value.ToString() ?? ""
    };

    private static Qc401MClassDto Read(NpgsqlConnection connection, NpgsqlTransaction? transaction, string no, bool forUpdate)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblqcmclassbasis WHERE lower(mclassno)=lower(@no){(forUpdate ? " FOR UPDATE" : "")};", connection, transaction);
        AddText(command, "no", no, 30);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, $"找不到主分類 {no}。");
        var result = Read(reader);
        if (reader.Read()) throw Rule(409, "主分類存在大小寫不同的重複編號，請先整理既有資料。");
        return result;
    }

    private static Qc401MClassDto Read(NpgsqlDataReader reader) => new(
        reader.GetString(0), Text(reader, 1), Text(reader, 2), Date(reader, 3), reader.GetInt32(4),
        reader.IsDBNull(5) ? null : reader.GetDecimal(5), Text(reader, 6), Date(reader, 7));

    private static void ChangeState(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc401MClassDto current, string actor, int next)
    {
        using var command = new NpgsqlCommand("""
            UPDATE "YMES".tblqcmclassbasis SET issuestate=@next,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
            WHERE mclassno=@no AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp;
            """, connection, transaction);
        BindExpected(command, current, actor); AddNumeric(command, "next", next);
        if (command.ExecuteNonQuery() != 1) throw Conflict();
    }

    private static void EnsureNoReferences(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        // The master row is locked before reference checks. Child APIs must take
        // a shared lock on this master when validating new class assignments.
        foreach (var table in ReferenceTables)
        {
            if (!TableExists(connection, transaction, table)) continue;
            using var command = new NpgsqlCommand($"SELECT 1 FROM \"YMES\".{table} WHERE lower(mclassno)=lower(@no) LIMIT 1;", connection, transaction);
            AddText(command, "no", no, 30);
            if (command.ExecuteScalar() is not null) throw Rule(409, "此主分類已被品質資料引用，不能實體刪除。");
        }
    }

    private static void EnsureExpected(Qc401MClassDto current, int? state, decimal? stamp)
    {
        if (state is null || state is not (-1 or 0 or 1 or 2)) throw Rule(400, "IssueState 必須提供，且僅允許 -1、0、1、2。");
        if (current.IssueState != state || current.DataStamp != stamp) throw Conflict();
    }

    private static string NormalizeKey(string? value)
    {
        var no = (value ?? "").Trim().ToUpperInvariant();
        if (no.Length is < 1 or > 30 || no.Any(char.IsControl)) throw Rule(400, "主分類編號為必填、最多 30 個字元，且不可含控制字元。");
        return no;
    }

    private static string? NormalizeDescription(string? value)
    {
        var description = (value ?? "").Trim();
        if (description.Length > 255 || description.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t')))
            throw Rule(400, "說明不可超過 255 個字元或含非換行控制字元。");
        return description.Length == 0 ? null : description;
    }

    private static string NormalizeActor(string value)
    {
        var actor = value.Trim();
        if (actor.Length is < 1 or > 10 || actor.Any(char.IsControl)) throw Rule(401, "登入帳號不可空白且不可超過 10 個字元。");
        return actor;
    }

    private static bool TableExists(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='YMES' AND c.relname=@table AND c.relkind IN ('r','p','v','m','f'));", connection, transaction);
        AddText(command, "table", table, 63);
        return command.ExecuteScalar() is true;
    }

    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction)
    {
        if (!TableExists(connection, transaction, "tblqcmclassbasis")) throw Rule(503, "YMES 尚未建立 TBLQCMCLASSBASIS；請先比對來源 DDL 並套用 QC401 migration。");
    }

    private static void BindExpected(NpgsqlCommand command, Qc401MClassDto current, string actor)
    {
        AddText(command, "no", current.MClassNo, 30); AddText(command, "actor", actor, 10);
        AddNumeric(command, "state", current.IssueState); AddNumeric(command, "stamp", current.DataStamp);
    }

    private static void AddText(NpgsqlCommand command, string name, string? value, int size)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Varchar, size);
        parameter.Value = (object?)value ?? DBNull.Value;
    }

    private static void AddNumeric(NpgsqlCommand command, string name, decimal? value)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Numeric);
        parameter.Value = (object?)value ?? DBNull.Value;
    }

    private static string? Text(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static DateTime? Date(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    private static void Rollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { /* Preserve original error. */ } }
    private static Qc401RuleException Rule(int status, string message) => new(status, message);
    private static Qc401RuleException Conflict() => Rule(409, "資料已被其他作業修改，請重新查詢後再試。");
}
