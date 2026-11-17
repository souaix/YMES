using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using ExcelDataReader;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

public sealed class Qc112Repository(
    NpgsqlDataSource dataSource,
    IYmesQc112FileStorage fileStorage,
    ILogger<Qc112Repository> logger)
{
    private const long MaxWorkbookBytes = 15L * 1024 * 1024;
    private const int MaxImportRows = 1000;

    public IReadOnlyList<Qc112ReasonDto> GetApprovedReasons()
    {
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("""
            SELECT reasonno, reasonname, reasonlevel::int, reasontype::int, reasonsubtype,
                   description, creator, createdate, reviser, revisedate,
                   issuestate::int, datastamp
            FROM "YMES".tblqcreasonbasis
            WHERE issuestate=2
            ORDER BY reasonno;
            """, connection);
        using var reader = command.ExecuteReader();
        var result = new List<Qc112ReasonDto>();
        while (reader.Read())
        {
            result.Add(new Qc112ReasonDto(
                reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3),
                NullableString(reader, 4), NullableString(reader, 5), NullableString(reader, 6),
                NullableDate(reader, 7), NullableString(reader, 8), NullableDate(reader, 9),
                reader.GetInt32(10), NullableDecimal(reader, 11)));
        }
        return result;
    }

    public IReadOnlyList<Qc112SopFileDto> GetFiles(string reasonNo)
    {
        ValidateText(reasonNo, "ReasonNo", 20);
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("""
            SELECT reasonno, productno, COALESCE(sopfilename,''), COALESCE(sopfilepath,'')
            FROM "YMES".tblqcreasonprdsopfile
            WHERE reasonno=@reasonNo
            ORDER BY productno;
            """, connection);
        AddVarchar(command, "reasonNo", reasonNo);
        using var reader = command.ExecuteReader();
        var result = new List<Qc112SopFileDto>();
        while (reader.Read())
            result.Add(new Qc112SopFileDto(reader.GetString(0), reader.GetString(1), reader.GetString(2), ToDisplayPath(reader.GetString(3))));
        return result;
    }

    public IReadOnlyList<Qc112ProductDto> FindApprovedCurrentProducts(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            throw new Qc112RuleException(StatusCodes.Status400BadRequest, "請輸入 ProductNo 查詢條件；輸入 * 可查詢全部核准中版本。");
        if (search.Length > 50)
            throw new Qc112RuleException(StatusCodes.Status400BadRequest, "ProductNo 查詢條件不可超過 50 個字元。");

        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("""
            SELECT productno, productname, issuestate::int, curversion
            FROM "YMES".tblprdproductbasis
            WHERE issuestate=2 AND curversion=TRUE
              AND (@search='*' OR position(lower(@search) in lower(productno)) > 0)
            ORDER BY productno;
            """, connection);
        AddVarchar(command, "search", search.Trim());
        using var reader = command.ExecuteReader();
        var result = new List<Qc112ProductDto>();
        while (reader.Read())
            result.Add(new Qc112ProductDto(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetBoolean(3)));
        return result;
    }

    public async Task<Qc112SopFileDto> AddFileAsync(
        string reasonNo,
        string productNo,
        string sopFileName,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        ValidateText(reasonNo, "ReasonNo", 20);
        ValidateText(productNo, "ProductNo", 50);
        ValidateFileName(sopFileName);
        if (file is null || file.Length == 0)
            throw new Qc112RuleException(StatusCodes.Status400BadRequest, "請選擇 SOP 附件。");

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        Qc112StoredFile? storedFile = null;
        try
        {
            await EnsureApprovedReasonAndProductAsync(connection, transaction, reasonNo, productNo, cancellationToken);
            await EnsureNoExistingBindingAsync(connection, transaction, reasonNo, productNo, cancellationToken);

            storedFile = await fileStorage.SaveAsync(file, sopFileName, cancellationToken);
            await using var command = new NpgsqlCommand("""
                INSERT INTO "YMES".tblqcreasonprdsopfile(reasonno, productno, sopfilename, sopfilepath)
                VALUES(@reasonNo, @productNo, @fileName, @filePath);
                """, connection, transaction);
            AddVarchar(command, "reasonNo", reasonNo);
            AddVarchar(command, "productNo", productNo);
            AddVarchar(command, "fileName", sopFileName);
            AddVarchar(command, "filePath", storedFile.DisplayPath);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new Qc112SopFileDto(reasonNo, productNo, sopFileName, storedFile.DisplayPath);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (storedFile is not null) fileStorage.Delete(storedFile.DisplayPath);
            throw new Qc112RuleException(StatusCodes.Status409Conflict, "此品質原因與產品已存在 SOP 對應。");
        }
        catch
        {
            try { await transaction.RollbackAsync(cancellationToken); } catch { /* retain the original failure */ }
            if (storedFile is not null)
            {
                try { fileStorage.Delete(storedFile.DisplayPath); }
                catch (Exception cleanupError) { logger.LogError(cleanupError, "QC112 failed to remove an unbound uploaded file {DisplayPath}", storedFile.DisplayPath); }
            }
            throw;
        }
    }

    public async Task RemoveFileAsync(string reasonNo, string productNo, CancellationToken cancellationToken)
    {
        ValidateText(reasonNo, "ReasonNo", 20);
        ValidateText(productNo, "ProductNo", 50);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        string? displayPath = null;
        string? pendingToken = null;
        try
        {
            await using (var select = new NpgsqlCommand("""
                SELECT sopfilepath FROM "YMES".tblqcreasonprdsopfile
                WHERE reasonno=@reasonNo AND productno=@productNo FOR UPDATE;
                """, connection, transaction))
            {
                AddVarchar(select, "reasonNo", reasonNo);
                AddVarchar(select, "productNo", productNo);
                var value = await select.ExecuteScalarAsync(cancellationToken);
                if (value is null) throw new Qc112RuleException(StatusCodes.Status404NotFound, "找不到此品質原因與產品的 SOP 對應。");
                displayPath = value is DBNull ? null : (string)value;
            }

            var isShared = false;
            if (!string.IsNullOrWhiteSpace(displayPath))
            {
                await using var references = new NpgsqlCommand("""
                    SELECT COUNT(*) FROM "YMES".tblqcreasonprdsopfile
                    WHERE sopfilepath=@filePath AND NOT (reasonno=@reasonNo AND productno=@productNo);
                    """, connection, transaction);
                AddVarchar(references, "filePath", displayPath);
                AddVarchar(references, "reasonNo", reasonNo);
                AddVarchar(references, "productNo", productNo);
                isShared = Convert.ToInt64(await references.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) > 0;
                if (!isShared) pendingToken = fileStorage.MoveToPendingDelete(displayPath);
            }

            await using (var delete = new NpgsqlCommand("""
                DELETE FROM "YMES".tblqcreasonprdsopfile
                WHERE reasonno=@reasonNo AND productno=@productNo;
                """, connection, transaction))
            {
                AddVarchar(delete, "reasonNo", reasonNo);
                AddVarchar(delete, "productNo", productNo);
                if (await delete.ExecuteNonQueryAsync(cancellationToken) == 0)
                    throw new Qc112RuleException(StatusCodes.Status404NotFound, "找不到此品質原因與產品的 SOP 對應。");
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            try { await transaction.RollbackAsync(cancellationToken); } catch { /* retain the original failure */ }
            if (pendingToken is not null && displayPath is not null)
            {
                try { fileStorage.RestorePendingDelete(pendingToken, displayPath); }
                catch (Exception restoreError) { logger.LogCritical(restoreError, "QC112 could not restore a staged attachment {DisplayPath}", displayPath); }
            }
            throw;
        }

        if (pendingToken is not null)
        {
            try { fileStorage.FinalizePendingDelete(pendingToken); }
            catch (Exception deleteError) { logger.LogError(deleteError, "QC112 removed the binding but could not purge staged attachment {DisplayPath}", displayPath); }
        }
    }

    public IReadOnlyList<Qc112ImportPreviewRow> PreviewImport(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new Qc112RuleException(StatusCodes.Status400BadRequest, "請選擇匯入工作簿。");
        if (file.Length > MaxWorkbookBytes)
            throw new Qc112RuleException(StatusCodes.Status413PayloadTooLarge, "匯入工作簿不可超過 15 MB。");

        var extension = Path.GetExtension(file.FileName);
        if (!new[] { ".xls", ".xlsx", ".csv" }.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new Qc112RuleException(StatusCodes.Status415UnsupportedMediaType, "匯入工作簿只允許 XLS、XLSX 或 CSV。");

        try
        {
            using var input = file.OpenReadStream();
            var rows = extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
                ? ReadCsvRows(input)
                : ReadExcelRows(input);
            if (rows.Count <= 1) return [];
            var preview = rows.Skip(1)
                .Select((row, index) => MapImportRow(index + 2, row))
                .Where(row => !string.IsNullOrWhiteSpace(row.ReasonNo) || !string.IsNullOrWhiteSpace(row.ProductNo) || !string.IsNullOrWhiteSpace(row.SopFileName) || !string.IsNullOrWhiteSpace(row.SopFilePath))
                .Take(MaxImportRows + 1)
                .ToArray();
            if (preview.Length > MaxImportRows)
                throw new Qc112RuleException(StatusCodes.Status400BadRequest, $"單次最多預覽 {MaxImportRows} 筆資料。");
            return preview;
        }
        catch (Qc112RuleException) { throw; }
        catch (Exception ex) when (ex is InvalidDataException or IOException or ArgumentException or FormatException)
        {
            throw new Qc112RuleException(StatusCodes.Status400BadRequest, "無法讀取工作簿；請確認檔案格式與第一張工作表內容。");
        }
    }

    private async Task EnsureApprovedReasonAndProductAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string reasonNo,
        string productNo,
        CancellationToken cancellationToken)
    {
        await using (var reason = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblqcreasonbasis WHERE reasonno=@no AND issuestate=2 FOR SHARE;", connection, transaction))
        {
            AddVarchar(reason, "no", reasonNo);
            if (await reason.ExecuteScalarAsync(cancellationToken) is null)
                throw new Qc112RuleException(StatusCodes.Status422UnprocessableEntity, "品質原因不存在或尚未核准。");
        }
        await using (var product = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblprdproductbasis WHERE productno=@no AND issuestate=2 AND curversion=TRUE FOR SHARE;", connection, transaction))
        {
            AddVarchar(product, "no", productNo);
            if (await product.ExecuteScalarAsync(cancellationToken) is null)
                throw new Qc112RuleException(StatusCodes.Status422UnprocessableEntity, "產品不存在、尚未核准或不是目前版本。");
        }
    }

    private static async Task EnsureNoExistingBindingAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string reasonNo, string productNo, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT 1 FROM \"YMES\".tblqcreasonprdsopfile WHERE reasonno=@reasonNo AND productno=@productNo FOR UPDATE;", connection, transaction);
        AddVarchar(command, "reasonNo", reasonNo);
        AddVarchar(command, "productNo", productNo);
        if (await command.ExecuteScalarAsync(cancellationToken) is not null)
            throw new Qc112RuleException(StatusCodes.Status409Conflict, "此品質原因與產品已存在 SOP 對應。");
    }

    private static IReadOnlyList<string[]> ReadExcelRows(Stream input)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var workbook = ExcelReaderFactory.CreateReader(input);
        var rows = new List<string[]>(MaxImportRows + 1);
        while (workbook.Read() && rows.Count <= MaxImportRows + 1)
        {
            rows.Add(Enumerable.Range(0, 4)
                .Select(column => column < workbook.FieldCount ? Convert.ToString(workbook.GetValue(column), CultureInfo.InvariantCulture)?.Trim() ?? "" : "")
                .ToArray());
        }
        return rows;
    }

    private static IReadOnlyList<string[]> ReadCsvRows(Stream input)
    {
        using var reader = new StreamReader(input, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        var allRows = new List<string[]>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var inQuotes = false;
        while (reader.Read() is var raw && raw >= 0)
        {
            var current = (char)raw;
            if (inQuotes)
            {
                if (current == '"' && reader.Peek() == '"') { reader.Read(); cell.Append('"'); }
                else if (current == '"') inQuotes = false;
                else cell.Append(current);
            }
            else if (current == '"' && cell.Length == 0) inQuotes = true;
            else if (current == ',') { row.Add(cell.ToString().Trim()); cell.Clear(); }
            else if (current is '\r' or '\n')
            {
                if (current == '\r' && reader.Peek() == '\n') reader.Read();
                row.Add(cell.ToString().Trim()); cell.Clear();
                allRows.Add(row.ToArray()); row.Clear();
                if (allRows.Count > MaxImportRows + 1) break;
            }
            else cell.Append(current);
        }
        if (inQuotes) throw new InvalidDataException("Unclosed CSV quoted field.");
        if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString().Trim()); allRows.Add(row.ToArray()); }
        return allRows;
    }

    private static Qc112ImportPreviewRow MapImportRow(int rowNumber, string[] row)
    {
        var reasonNo = Cell(row, 0);
        var productNo = Cell(row, 1);
        var fileName = Cell(row, 2);
        var filePath = Cell(row, 3);
        var error = string.IsNullOrWhiteSpace(reasonNo) || string.IsNullOrWhiteSpace(productNo) || string.IsNullOrWhiteSpace(fileName)
            ? "ReasonNo、ProductNo、FileName 為必填。"
            : fileName.Length > 100 || filePath.Length > 2000
                ? "FileName 或 FilePath 超過資料欄位長度。"
                : "";
        return new Qc112ImportPreviewRow(rowNumber, reasonNo, productNo, fileName, filePath, error.Length == 0 ? "待處理" : "失敗", error);
    }

    private static string Cell(string[] row, int index) => index < row.Length ? row[index].Trim() : "";

    private static string ToDisplayPath(string storedPath)
    {
        if (storedPath.StartsWith("QC112/", StringComparison.Ordinal)) return storedPath;
        var filename = storedPath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "附件";
        return $"QC112/legacy/{filename}";
    }

    private static void ValidateText(string value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength || value.Any(char.IsControl))
            throw new Qc112RuleException(StatusCodes.Status400BadRequest, $"{field} 為必填，且不可超過 {maxLength} 個字元。");
    }

    private static void ValidateFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 100 || value.Contains('/') || value.Contains('\\') || value.Any(char.IsControl))
            throw new Qc112RuleException(StatusCodes.Status400BadRequest, "FileName 不可空白、超過 100 個字元或包含路徑分隔字元。");
    }

    private static string? NullableString(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static DateTime? NullableDate(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    private static decimal? NullableDecimal(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    private static void AddVarchar(NpgsqlCommand command, string name, string? value) => command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;
}
