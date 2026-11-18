using System.Globalization;
using System.IO.Compression;
using System.Text;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc402Repository(NpgsqlDataSource dataSource)
{
    private const string Columns = "sclassno,mclassno,description,creator,createdate,issuestate::int,datastamp,reviser,revisedate";
    private const int MaxImportRows = 10000;
    private const long MaxImportBytes = 5 * 1024 * 1024;

    public IReadOnlyList<Qc402SClassDto> List(string? searchFieldName, string? searchFieldValue)
    {
        if (!string.IsNullOrWhiteSpace(searchFieldName) && !string.Equals(searchFieldName.Trim(), "SClassNo", StringComparison.OrdinalIgnoreCase))
            throw Rule(400, "查詢欄位只允許 SClassNo。");
        var search = (searchFieldValue ?? "").Trim();
        if (search.Length > 30 || search.Any(char.IsControl)) throw Rule(400, "次分類查詢不可超過 30 個字元或含控制字元。");
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcsclassbasis");
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblqcsclassbasis WHERE @search='' OR position(lower(@search) in lower(sclassno))>0 ORDER BY sclassno;", connection);
        AddText(command, "search", search, 30);
        var result = new List<Qc402SClassDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(Read(reader));
        return result;
    }

    public IReadOnlyList<Qc402MClassOption> MClassOptions()
    {
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcmclassbasis");
        using var command = new NpgsqlCommand("SELECT mclassno,description,issuestate::int FROM \"YMES\".tblqcmclassbasis WHERE issuestate=2 ORDER BY mclassno;", connection);
        var result = new List<Qc402MClassOption>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) result.Add(new(reader.GetString(0), Text(reader, 1), reader.GetInt32(2)));
        return result;
    }

    public Qc402SClassDto Get(string key)
    {
        var no = NormalizeKey(key, "次分類");
        using var connection = dataSource.OpenConnection();
        EnsureTable(connection, null, "tblqcsclassbasis");
        return Read(connection, null, no, false);
    }

    public Qc402SClassDto Create(Qc402SClassWriteRequest request, string actor)
    {
        actor = NormalizeActor(actor);
        var no = NormalizeKey(request.SClassNo, "次分類");
        var parent = NormalizeKey(request.MClassNo, "主分類");
        var description = NormalizeDescription(request.Description);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblqcsclassbasis");
            using (var duplicate = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblqcsclassbasis WHERE lower(sclassno)=lower(@no);", connection, transaction))
            {
                AddText(duplicate, "no", no, 30);
                if (duplicate.ExecuteScalar() is not null) throw Rule(409, "次分類編號已存在。");
            }
            parent = LockApprovedParent(connection, transaction, parent);
            using var command = new NpgsqlCommand($"""
                INSERT INTO "YMES".tblqcsclassbasis(sclassno,mclassno,description,creator,createdate,issuestate)
                VALUES(@no,@parent,@description,@actor,now(),0)
                RETURNING {Columns};
                """, connection, transaction);
            AddText(command, "no", no, 30); AddText(command, "parent", parent, 30);
            AddText(command, "description", description, 255); AddText(command, "actor", actor, 10);
            var result = ReadReturned(command);
            transaction.Commit();
            return result;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            Rollback(transaction); throw Rule(409, "次分類編號已存在。");
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc402SClassDto Update(string key, Qc402SClassWriteRequest request, int? issueState, decimal? stamp, string actor)
    {
        actor = NormalizeActor(actor);
        var no = NormalizeKey(key, "次分類");
        if (!string.IsNullOrWhiteSpace(request.SClassNo) && !string.Equals(no, NormalizeKey(request.SClassNo, "次分類"), StringComparison.Ordinal))
            throw Rule(400, "次分類編號不可修改。");
        var parent = NormalizeKey(request.MClassNo, "主分類");
        var description = NormalizeDescription(request.Description);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblqcsclassbasis");
            var current = Read(connection, transaction, no, true);
            EnsureExpected(current, issueState, stamp);
            if (current.IssueState is not (0 or 2)) throw Rule(409, "只有未簽核或已簽核的次分類資料可修改。");
            parent = LockApprovedParent(connection, transaction, parent);
            using var command = new NpgsqlCommand($"""
                UPDATE "YMES".tblqcsclassbasis
                SET mclassno=@parent,description=@description,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
                WHERE sclassno=@no AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp
                RETURNING {Columns};
                """, connection, transaction);
            BindExpected(command, current, actor);
            AddText(command, "parent", parent, 30); AddText(command, "description", description, 255);
            var result = ReadReturned(command);
            transaction.Commit();
            return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc402SClassDto Delete(string key, int? issueState, decimal? stamp, string actor)
    {
        actor = NormalizeActor(actor);
        var no = NormalizeKey(key, "次分類");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblqcsclassbasis");
            var current = Read(connection, transaction, no, true);
            EnsureExpected(current, issueState, stamp);
            if (current.IssueState == 1) throw Rule(409, "簽核中的次分類不可刪除。");
            if (current.IssueState == 2)
            {
                var result = ChangeState(connection, transaction, current, actor, -1);
                transaction.Commit(); return result;
            }
            if (current.IssueState is not (0 or -1)) throw Rule(409, "此次分類狀態不允許刪除。");
            EnsureNoReferences(connection, transaction, current.SClassNo);
            using var command = new NpgsqlCommand("DELETE FROM \"YMES\".tblqcsclassbasis WHERE sclassno=@no AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp;", connection, transaction);
            BindExpected(command, current, actor);
            if (command.ExecuteNonQuery() != 1) throw Conflict();
            transaction.Commit(); return current;
        }
        catch { Rollback(transaction); throw; }
    }

    public Qc402SClassDto Approve(string key, int? issueState, decimal? stamp, string actor)
    {
        actor = NormalizeActor(actor);
        var no = NormalizeKey(key, "次分類");
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            EnsureTable(connection, transaction, "tblqcsclassbasis");
            var current = Read(connection, transaction, no, true);
            EnsureExpected(current, issueState, stamp);
            if (current.IssueState != 0) throw Rule(409, "只有未簽核的次分類資料可核准。");
            if (current.MClassNo is null) throw Rule(422, "此次分類尚未指定主分類，不能核准。");
            LockApprovedParent(connection, transaction, current.MClassNo);
            if (new[] { "tblisujobbasis", "tblisujobdetail", "tblissuejobbasis", "tblissuejobdetail" }.Any(table => TableExists(connection, transaction, table)))
                throw Rule(503, "YMES 已存在 Issue/Job 流程表，但 QC402 尚未接入共用簽核服務；資料未變更。");
            var result = ChangeState(connection, transaction, current, actor, 2);
            transaction.Commit(); return result;
        }
        catch { Rollback(transaction); throw; }
    }

    public IReadOnlyList<Qc402ImportPair> Preview(IFormFile? file, CancellationToken cancellationToken)
    {
        var pairs = ReadImportRows(file, cancellationToken);
        var seen = new HashSet<(string, string)>();
        // Preview folds equal SClass/MClass pairs, regardless of Description.
        // Import deliberately reparses the raw rows before grouping SClassNo.
        return pairs.Where(pair => seen.Add((pair.SClassNo.ToUpperInvariant(), pair.MClassNo.ToUpperInvariant()))).ToArray();
    }

    public IReadOnlyList<Qc402ImportResult> Import(IFormFile? file, string actor, CancellationToken cancellationToken)
    {
        actor = NormalizeActor(actor);
        var rawRows = ReadImportRows(file, cancellationToken);
        var results = new List<Qc402ImportResult>();
        var groups = rawRows.GroupBy(x => x.SClassNo, StringComparer.OrdinalIgnoreCase).Select(group => group.ToArray()).ToArray();
        void AddUnprocessed(int start, string message)
        {
            foreach (var remaining in groups.Skip(start))
                results.AddRange(DistinctPairs(remaining).Select(x => new Qc402ImportResult(x.SClassNo, x.MClassNo, x.Description, "fail", message)));
        }
        for (var index = 0; index < groups.Length; index++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                AddUnprocessed(index, "已取消後續匯入；此筆尚未處理。");
                break;
            }
            var items = groups[index];
            if (items.Length > 1)
            {
                results.AddRange(DistinctPairs(items).Select(x => new Qc402ImportResult(x.SClassNo, x.MClassNo, x.Description, "fail", "同一 SClassNo 有多筆資料。")));
                continue;
            }
            var item = items[0];
            try
            {
                var row = Create(new Qc402SClassWriteRequest { SClassNo = item.SClassNo, MClassNo = item.MClassNo, Description = item.Description }, actor);
                results.Add(new(row.SClassNo, row.MClassNo ?? item.MClassNo, item.Description, "success", ""));
            }
            catch (Qc402RuleException ex)
            {
                results.Add(new(item.SClassNo, item.MClassNo, item.Description, "fail", ex.Message));
                if (ex.StatusCode >= 500)
                {
                    AddUnprocessed(index + 1, "服務暫時無法使用，匯入已停止；此筆尚未處理。");
                    break;
                }
            }
            catch (NpgsqlException)
            {
                // Earlier groups have committed independently. Preserve those
                // results if a later connection/commit fails; an interrupted
                // commit may require read-back to establish the current row.
                results.Add(new(item.SClassNo, item.MClassNo, item.Description, "fail", "資料庫連線或操作中斷，此筆結果無法確認，請重新查詢；後續匯入已停止。"));
                AddUnprocessed(index + 1, "資料庫操作中斷，匯入已停止；此筆尚未處理。");
                break;
            }
        }
        return results;
    }

    private static IReadOnlyList<Qc402ImportPair> ReadImportRows(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) throw Rule(400, "請選擇 .xls 或 .xlsx 檔案。");
        if (file.Length > MaxImportBytes) throw Rule(413, "次分類匯入檔不可超過 5 MiB。");
        var extension = Path.GetExtension(file.FileName);
        var xlsx = extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase);
        if (!xlsx && !extension.Equals(".xls", StringComparison.OrdinalIgnoreCase)) throw Rule(415, "正式次分類匯入只接受 .xls 或 .xlsx。");
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var input = new MemoryStream();
            file.CopyTo(input);
            if (input.Length > MaxImportBytes) throw Rule(413, "次分類匯入檔不可超過 5 MiB。");
            input.Position = 0;
            if (xlsx)
            {
                using var archive = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
                if (archive.Entries.Count > 2048 || archive.Entries.Sum(x => x.Length) > 64L * 1024 * 1024)
                    throw Rule(413, "Excel 解壓後的大小或檔案數超過匯入限制。");
            }
            input.Position = 0;
            using var workbook = ExcelReaderFactory.CreateReader(input);
            if (!workbook.Read() || workbook.FieldCount < 2) throw Rule(400, "Excel 第一個工作表必須包含標題列及前兩個必要欄位。");
            var result = new List<Qc402ImportPair>();
            var rowNo = 1;
            while (workbook.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (++rowNo > MaxImportRows + 1) throw Rule(400, $"單次最多匯入 {MaxImportRows} 個資料列。");
                var no = CellText(workbook.GetValue(0)).Trim();
                var parent = CellText(workbook.GetValue(1)).Trim();
                var description = workbook.FieldCount > 2 ? CellText(workbook.GetValue(2)).Trim() : "";
                if (no.Length == 0 || parent.Length == 0) throw Rule(400, $"第 {rowNo} 列的次分類編號與主分類編號必填。");
                result.Add(new(no, parent, description));
            }
            if (result.Count == 0) throw Rule(400, "Excel 第一個工作表沒有次分類資料列。");
            return result;
        }
        catch (Qc402RuleException) { throw; }
        catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or FormatException or NotSupportedException or System.Xml.XmlException
                                   || ex.GetType().Namespace == "ExcelDataReader.Exceptions")
        {
            throw Rule(400, "無法讀取 Excel，請確認檔案有效、未加密且第一個工作表可讀取；公式欄位需已有 Excel 儲存的計算結果。");
        }
    }

    private static IEnumerable<Qc402ImportPair> DistinctPairs(IEnumerable<Qc402ImportPair> rows)
    {
        var seen = new HashSet<(string, string)>();
        return rows.Where(pair => seen.Add((pair.SClassNo.ToUpperInvariant(), pair.MClassNo.ToUpperInvariant())));
    }

    private static string CellText(object? value) => value switch
    {
        null or DBNull => "",
        DateTime time => time.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture),
        IFormattable formatted => formatted.ToString(null, CultureInfo.InvariantCulture) ?? "",
        _ => value.ToString() ?? ""
    };

    private static string LockApprovedParent(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        EnsureTable(connection, transaction, "tblqcmclassbasis");
        using var command = new NpgsqlCommand("SELECT mclassno,issuestate::int FROM \"YMES\".tblqcmclassbasis WHERE lower(mclassno)=lower(@parent) FOR SHARE;", connection, transaction);
        AddText(command, "parent", no, 30);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(422, "找不到所選的主分類，請重新選取。");
        var storedNo = reader.GetString(0);
        var state = reader.GetInt32(1);
        if (reader.Read()) throw Rule(409, "主分類存在大小寫不同的重複編號，請先整理資料。");
        if (state != 2) throw Rule(422, "主分類必須是已簽核資料。");
        return storedNo;
    }

    private static Qc402SClassDto Read(NpgsqlConnection connection, NpgsqlTransaction? transaction, string no, bool forUpdate)
    {
        using var command = new NpgsqlCommand($"SELECT {Columns} FROM \"YMES\".tblqcsclassbasis WHERE lower(sclassno)=lower(@no){(forUpdate ? " FOR UPDATE" : "")};", connection, transaction);
        AddText(command, "no", no, 30);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Rule(404, $"找不到次分類 {no}。");
        var result = Read(reader);
        if (reader.Read()) throw Rule(409, "次分類存在大小寫不同的重複編號，請先整理資料。");
        return result;
    }

    private static Qc402SClassDto ReadReturned(NpgsqlCommand command)
    {
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw Conflict();
        return Read(reader);
    }

    private static Qc402SClassDto Read(NpgsqlDataReader reader) => new(
        reader.GetString(0), Text(reader, 1), Text(reader, 2), Text(reader, 3), Date(reader, 4), reader.GetInt32(5),
        reader.IsDBNull(6) ? null : reader.GetDecimal(6), Text(reader, 7), Date(reader, 8));

    private static Qc402SClassDto ChangeState(NpgsqlConnection connection, NpgsqlTransaction transaction, Qc402SClassDto current, string actor, int next)
    {
        using var command = new NpgsqlCommand($"""
            UPDATE "YMES".tblqcsclassbasis SET issuestate=@next,reviser=@actor,revisedate=now(),datastamp=COALESCE(datastamp,0)+1
            WHERE sclassno=@no AND issuestate=@state AND datastamp IS NOT DISTINCT FROM @stamp
            RETURNING {Columns};
            """, connection, transaction);
        BindExpected(command, current, actor); AddNumeric(command, "next", next);
        return ReadReturned(command);
    }

    private static void EnsureNoReferences(NpgsqlConnection connection, NpgsqlTransaction transaction, string no)
    {
        // Only the verified QC Hold Item schema is included. Do not invent
        // cascades into downstream QC/WIP data. Child writers must FOR SHARE
        // this SClass row while assigning it, coordinating with this row lock.
        if (!TableExists(connection, transaction, "tblqcholditembasis")) return;
        using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblqcholditembasis WHERE lower(sclassno)=lower(@no) LIMIT 1;", connection, transaction);
        AddText(command, "no", no, 30);
        if (command.ExecuteScalar() is not null) throw Rule(409, "此次分類已被品質異常原因引用，不能實體刪除。");
    }

    private static void EnsureExpected(Qc402SClassDto current, int? state, decimal? stamp)
    {
        if (state is null || state is not (-1 or 0 or 1 or 2)) throw Rule(400, "IssueState 必須提供，且僅允許 -1、0、1、2。");
        if (current.IssueState != state || current.DataStamp != stamp) throw Conflict();
    }

    private static string NormalizeKey(string? value, string field)
    {
        var no = (value ?? "").Trim().ToUpperInvariant();
        if (no.Length is < 1 or > 30 || no.Any(char.IsControl)) throw Rule(400, $"{field}編號為必填、最多 30 個字元，且不可含控制字元。");
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
        using var command = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema='YMES' AND table_name=@table);", connection, transaction);
        AddText(command, "table", table, 63);
        return command.ExecuteScalar() is true;
    }

    private static void EnsureTable(NpgsqlConnection connection, NpgsqlTransaction? transaction, string table)
    {
        if (!TableExists(connection, transaction, table)) throw Rule(503, $"YMES 尚未建立 {table.ToUpperInvariant()}；請先比對來源 DDL 並套用 migration。");
    }

    private static void BindExpected(NpgsqlCommand command, Qc402SClassDto current, string actor)
    {
        AddText(command, "no", current.SClassNo, 30); AddText(command, "actor", actor, 10);
        AddNumeric(command, "state", current.IssueState); AddNumeric(command, "stamp", current.DataStamp);
    }

    private static void AddText(NpgsqlCommand command, string name, string? value, int size) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, size).Value = (object?)value ?? DBNull.Value;
    private static void AddNumeric(NpgsqlCommand command, string name, decimal? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;
    private static string? Text(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static DateTime? Date(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    private static void Rollback(NpgsqlTransaction transaction) { try { transaction.Rollback(); } catch { /* Preserve original error. */ } }
    private static Qc402RuleException Rule(int status, string message) => new(status, message);
    private static Qc402RuleException Conflict() => Rule(409, "資料已被其他作業修改，請重新查詢後再試。");
}
