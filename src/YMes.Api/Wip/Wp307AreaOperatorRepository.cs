using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed class Wp307AreaOperatorRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 10_000;
    private const int MaxPageSize = 100;
    private const int MaxOptions = 1_000;
    private static readonly TimeZoneInfo TaipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    public async Task<Wp307FilterOptionsResponse> GetFilterOptionsAsync(
        string areaNo, Wp307FilterOptionsQuery query, CancellationToken cancellationToken)
    {
        var normalizedArea = RequiredText(areaNo, 20, "AreaNo");
        var requestedDepartment = OptionalText(query.DepartmentNo, 20, "DepartmentNo");
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await BeginReadOnlyAsync(connection, cancellationToken);
        var departmentNo = await ResolveDepartmentAsync(connection, transaction, normalizedArea, requestedDepartment, cancellationToken);

        var shifts = new List<Wp307ShiftOption>();
        await using (var command = new NpgsqlCommand("""
            SELECT shiftno, shiftname, fromtime, totime
            FROM "YMES".tblusrshiftbasis
            WHERE departmentno = @departmentNo AND issuestate = 2
            ORDER BY shiftno COLLATE "C"
            LIMIT @take;
            """, connection, transaction))
        {
            command.Parameters.Add("departmentNo", NpgsqlDbType.Varchar).Value = departmentNo;
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxOptions + 1;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var shiftNo = reader.GetString(0);
                var shiftName = reader.GetString(1);
                shifts.Add(new Wp307ShiftOption(
                    shiftNo,
                    string.IsNullOrWhiteSpace(shiftName) ? shiftNo : $"{shiftNo}　{shiftName}",
                    shiftNo,
                    shiftName,
                    reader.IsDBNull(2) ? null : Unspecified(reader.GetDateTime(2)),
                    reader.IsDBNull(3) ? null : Unspecified(reader.GetDateTime(3))));
            }
        }
        if (shifts.Count > MaxOptions) throw TooMany("核准班別超過單次回傳上限，請縮小部門範圍。");

        var users = new List<Wp307UserOption>();
        await using (var command = new NpgsqlCommand("""
            SELECT userno, username
            FROM "YMES".tblusruserbasis
            WHERE departmentno = @departmentNo AND issuestate = 2
            ORDER BY userno COLLATE "C"
            LIMIT @take;
            """, connection, transaction))
        {
            command.Parameters.Add("departmentNo", NpgsqlDbType.Varchar).Value = departmentNo;
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxOptions + 1;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                users.Add(new Wp307UserOption(reader.GetString(0), reader.GetString(1)));
        }
        if (users.Count > MaxOptions) throw TooMany("核准使用者超過單次回傳上限，請縮小部門範圍。");

        await transaction.CommitAsync(cancellationToken);
        return new(normalizedArea, departmentNo, shifts, users);
    }

    public async Task<Wp307OperatorEligibleResponse> CheckEligibleAsync(
        string areaNo, string? userNo, CancellationToken cancellationToken)
    {
        var normalizedArea = RequiredText(areaNo, 20, "AreaNo");
        var normalizedUser = RequiredText(userNo, 10, "UserNo").ToUpperInvariant();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await BeginReadOnlyAsync(connection, cancellationToken);
        var departmentNo = await ResolveDepartmentAsync(connection, transaction, normalizedArea, null, cancellationToken);

        await using (var command = new NpgsqlCommand("""
            SELECT areano, departmentno, shiftno
            FROM "YMES".tblwipareaoperatorstate
            WHERE userno = @userNo;
            """, connection, transaction))
        {
            command.Parameters.Add("userNo", NpgsqlDbType.Varchar).Value = normalizedUser;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var activeArea = reader.GetString(0);
                var activeDepartment = reader.GetString(1);
                var activeShift = reader.GetString(2);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return new(false, activeDepartment, [], activeArea, activeShift);
            }
        }

        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TaipeiTimeZone).DateTime.TimeOfDay;
        var eligibleShifts = new List<string>();
        await using (var command = new NpgsqlCommand("""
            SELECT areaOperator.shiftno, userShift.fromtime, userShift.totime
            FROM "YMES".tblsmdareaoperator AS areaOperator
            JOIN "YMES".tblusrshiftbasis AS userShift
              ON userShift.departmentno = areaOperator.departmentno
             AND userShift.shiftno = areaOperator.shiftno
             AND userShift.issuestate = 2
            JOIN "YMES".tblusruserbasis AS userBasis
              ON userBasis.userno = areaOperator.userno
             AND userBasis.departmentno = areaOperator.departmentno
             AND userBasis.issuestate = 2
            WHERE areaOperator.areano = @areaNo
              AND areaOperator.departmentno = @departmentNo
              AND areaOperator.userno = @userNo
            ORDER BY areaOperator.shiftno COLLATE "C";
            """, connection, transaction))
        {
            command.Parameters.Add("areaNo", NpgsqlDbType.Varchar).Value = normalizedArea;
            command.Parameters.Add("departmentNo", NpgsqlDbType.Varchar).Value = departmentNo;
            command.Parameters.Add("userNo", NpgsqlDbType.Varchar).Value = normalizedUser;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.IsDBNull(1) || reader.IsDBNull(2)) continue;
                var from = reader.GetDateTime(1).TimeOfDay;
                var to = reader.GetDateTime(2).TimeOfDay;
                var inShift = from <= to ? now >= from && now <= to : now >= from || now <= to;
                if (inShift) eligibleShifts.Add(reader.GetString(0));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return new(eligibleShifts.Count != 0, departmentNo, eligibleShifts);
    }

    public async Task<Wp307Page<Wp307OperatorStateRow>> GetCurrentAsync(
        string areaNo, Wp307CurrentQuery query, CancellationToken cancellationToken)
    {
        var normalizedArea = RequiredText(areaNo, 20, "AreaNo");
        var requestedDepartment = RequiredText(query.DepartmentNo, 20, "DepartmentNo");
        var normalizedShift = OptionalText(query.ShiftNo, 20, "ShiftNo");
        var normalizedUser = OptionalText(query.UserNo, 10, "UserNo")?.ToUpperInvariant();
        var start = ToTaipeiWallTime(query.StartDate);
        var end = ToTaipeiWallTime(query.EndDate);
        ValidateRange(start, end, "StartDate must be earlier than EndDate.");
        var page = NormalizePage(query.Page, query.PageSize);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await BeginReadOnlyAsync(connection, cancellationToken);
        var departmentNo = await ResolveDepartmentAsync(connection, transaction, normalizedArea, requestedDepartment, cancellationToken);
        var rows = new List<Wp307OperatorStateRow>(page.PageSize + 1);

        await using (var command = new NpgsqlCommand("""
            SELECT areano, departmentno, shiftno, userno, logincomment, logintime
            FROM "YMES".tblwipareaoperatorstate
            WHERE areano = @areaNo
              AND departmentno = @departmentNo
              AND (@shiftNo IS NULL OR shiftno = @shiftNo)
              AND (@userNo IS NULL OR userno = @userNo)
              AND (@startDate IS NULL OR logintime > @startDate)
              AND (@endDate IS NULL OR logintime < @endDate)
            ORDER BY logintime DESC, userno COLLATE "C"
            LIMIT @take OFFSET @offset;
            """, connection, transaction))
        {
            AddCurrentFilters(command, normalizedArea, departmentNo, normalizedShift, normalizedUser, start, end, page);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4), Unspecified(reader.GetDateTime(5))));
        }

        var result = FinishPage(page, rows);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<Wp307Page<Wp307OperatorHistoryRow>> GetHistoryAsync(
        string areaNo, Wp307HistoryQuery query, CancellationToken cancellationToken)
    {
        var normalizedArea = RequiredText(areaNo, 20, "AreaNo");
        var requestedDepartment = RequiredText(query.DepartmentNo, 20, "DepartmentNo");
        var normalizedShift = OptionalText(query.ShiftNo, 20, "ShiftNo");
        var normalizedUser = OptionalText(query.UserNo, 10, "UserNo")?.ToUpperInvariant();
        var loginStart = ToTaipeiWallTime(query.LoginStartDate);
        var loginEnd = ToTaipeiWallTime(query.LoginEndDate);
        var logoutStart = ToTaipeiWallTime(query.LogoutStartDate);
        var logoutEnd = ToTaipeiWallTime(query.LogoutEndDate);
        ValidateRange(loginStart, loginEnd, "LoginStartDate must be earlier than LoginEndDate.");
        ValidateRange(logoutStart, logoutEnd, "LogoutStartDate must be earlier than LogoutEndDate.");
        if (query.ExceptionTime is < 0 or > 9999) throw Bad("ExceptionTime 必須介於 0 到 9999。");
        var page = NormalizePage(query.Page, query.PageSize);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await BeginReadOnlyAsync(connection, cancellationToken);
        var departmentNo = await ResolveDepartmentAsync(connection, transaction, normalizedArea, requestedDepartment, cancellationToken);
        var rows = new List<Wp307OperatorHistoryRow>(page.PageSize + 1);

        await using (var command = new NpgsqlCommand("""
            SELECT areano, departmentno, shiftno, userno, logintime, logouttime,
                   exceptiontime, logincomment, logoutcomment
            FROM "YMES".tblwipareaoperatorlog
            WHERE areano = @areaNo
              AND departmentno = @departmentNo
              AND (@shiftNo IS NULL OR shiftno = @shiftNo)
              AND (@userNo IS NULL OR userno = @userNo)
              AND (@exceptionTime IS NULL OR exceptiontime = @exceptionTime)
              AND (@loginStart IS NULL OR logintime >= @loginStart)
              AND (@loginEnd IS NULL OR logintime < @loginEnd)
              AND (@logoutStart IS NULL OR logouttime >= @logoutStart)
              AND (@logoutEnd IS NULL OR logouttime < @logoutEnd)
            ORDER BY logouttime DESC, logintime DESC, userno COLLATE "C",
                     shiftno COLLATE "C", exceptiontime,
                     COALESCE(logincomment, '') COLLATE "C",
                     COALESCE(logoutcomment, '') COLLATE "C"
            LIMIT @take OFFSET @offset;
            """, connection, transaction))
        {
            command.Parameters.Add("areaNo", NpgsqlDbType.Varchar).Value = normalizedArea;
            command.Parameters.Add("departmentNo", NpgsqlDbType.Varchar).Value = departmentNo;
            AddNullableText(command, "shiftNo", normalizedShift);
            AddNullableText(command, "userNo", normalizedUser);
            AddNullableDecimal(command, "exceptionTime", query.ExceptionTime);
            AddNullableTimestamp(command, "loginStart", loginStart);
            AddNullableTimestamp(command, "loginEnd", loginEnd);
            AddNullableTimestamp(command, "logoutStart", logoutStart);
            AddNullableTimestamp(command, "logoutEnd", logoutEnd);
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    Unspecified(reader.GetDateTime(4)), Unspecified(reader.GetDateTime(5)), reader.GetDecimal(6),
                    reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8)));
        }

        var result = FinishPage(page, rows);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static async Task<NpgsqlTransaction> BeginReadOnlyAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await using var command = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return transaction;
    }

    private static async Task<string> ResolveDepartmentAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string areaNo,
        string? requestedDepartment,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT departmentno
            FROM "YMES".tblsmdareabasis
            WHERE areano = @areaNo AND issuestate = 2;
            """, connection, transaction);
        command.Parameters.Add("areaNo", NpgsqlDbType.Varchar).Value = areaNo;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull) throw new Wp307AreaOperatorRuleException(404, "找不到已核准的區域。");
        var departmentNo = (string)result;
        if (string.IsNullOrWhiteSpace(departmentNo))
            throw new Wp307AreaOperatorRuleException(409, "此區域尚未設定部門。");
        departmentNo = departmentNo.Trim();
        if (requestedDepartment is not null && !string.Equals(requestedDepartment, departmentNo, StringComparison.Ordinal))
            throw Bad("DepartmentNo 與區域設定不符。");
        return departmentNo;
    }

    private static void AddCurrentFilters(
        NpgsqlCommand command, string areaNo, string departmentNo, string? shiftNo,
        string? userNo, DateTime? start, DateTime? end, NormalizedPage page)
    {
        command.Parameters.Add("areaNo", NpgsqlDbType.Varchar).Value = areaNo;
        command.Parameters.Add("departmentNo", NpgsqlDbType.Varchar).Value = departmentNo;
        AddNullableText(command, "shiftNo", shiftNo);
        AddNullableText(command, "userNo", userNo);
        AddNullableTimestamp(command, "startDate", start);
        AddNullableTimestamp(command, "endDate", end);
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
        command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;
    }

    private static void AddNullableText(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static void AddNullableDecimal(NpgsqlCommand command, string name, decimal? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = (object?)value ?? DBNull.Value;

    private static void AddNullableTimestamp(NpgsqlCommand command, string name, DateTime? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Timestamp).Value = (object?)value ?? DBNull.Value;

    private static DateTime? ToTaipeiWallTime(DateTimeOffset? value) => value is null
        ? null
        : DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(value.Value, TaipeiTimeZone).DateTime, DateTimeKind.Unspecified);

    private static DateTime Unspecified(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    private static void ValidateRange(DateTime? start, DateTime? end, string message)
    {
        if (start.HasValue && end.HasValue && start.Value >= end.Value) throw Bad(message);
    }

    private static NormalizedPage NormalizePage(int? page, int? pageSize)
    {
        var normalizedPage = page ?? 1;
        var normalizedPageSize = pageSize ?? 100;
        if (normalizedPage is < 1 or > MaxPage) throw Bad($"頁碼必須介於 1 到 {MaxPage:N0}。");
        if (normalizedPageSize is < 1 or > MaxPageSize) throw Bad($"每頁筆數必須介於 1 到 {MaxPageSize}。");
        return new(normalizedPage, normalizedPageSize, checked((normalizedPage - 1) * normalizedPageSize));
    }

    private static Wp307Page<T> FinishPage<T>(NormalizedPage page, List<T> rows)
    {
        var hasMore = rows.Count > page.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        return new(page.Page, page.PageSize, hasMore, rows);
    }

    private static string RequiredText(string? value, int maxLength, string field)
    {
        var normalized = OptionalText(value, maxLength, field);
        return normalized ?? throw Bad($"{field} 為必填欄位。");
    }

    private static string? OptionalText(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        try { _ = new UTF8Encoding(false, true).GetByteCount(normalized); }
        catch (EncoderFallbackException) { throw Bad($"{field} 含無效 Unicode。"); }
        if (normalized.Contains('\0') || normalized.Any(char.IsControl) || normalized.EnumerateRunes().Count() > maxLength)
            throw Bad($"{field} 長度或字元不正確（最多 {maxLength} 字元）。");
        return normalized;
    }

    private static Wp307AreaOperatorRuleException Bad(string message) => new(400, message);
    private static Wp307AreaOperatorRuleException TooMany(string message) => new(413, message);
    private sealed record NormalizedPage(int Page, int PageSize, int Offset);
}

public sealed class Wp307AreaOperatorRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
