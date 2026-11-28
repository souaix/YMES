using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed class Wp303EquipmentConsumeStatusRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 10_000;
    private const int MaxPageSize = 100;
    private const int MaxDeleteCount = 100;
    private static readonly TimeZoneInfo TaipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    public async Task<Wp303EquipmentConsumeStatusPage<Wp303EquipmentConsumeStatusItem>> ListAsync(
        Wp303EquipmentConsumeStatusQuery query, CancellationToken token)
    {
        var page = NormalizePage(query.Page, query.PageSize);
        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        await ExecuteAsync(connection, transaction, "SET TRANSACTION READ ONLY;", token);
        long totalCount;
        await using (var count = new NpgsqlCommand("SELECT count(*) FROM \"YMES\".tblwipeqpconsumestatus;", connection, transaction))
            totalCount = (long)(await count.ExecuteScalarAsync(token) ?? 0L);

        var rows = new List<Wp303EquipmentConsumeStatusItem>(page.PageSize + 1);
        await using (var command = new NpgsqlCommand("""
            SELECT equipmentno, consumeno, consumeseq, curqty, curperiod,
                   creator, createdate, revisor, revisedate
            FROM "YMES".tblwipeqpconsumestatus
            ORDER BY equipmentno COLLATE "C", consumeno COLLATE "C"
            LIMIT @take OFFSET @offset;
            """, connection, transaction))
        {
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token)) rows.Add(MapStatus(reader));
        }

        var result = FinishPage(page, rows) with { TotalCount = totalCount };
        await transaction.CommitAsync(token);
        return result;
    }

    public async Task<Wp303EquipmentConsumeStatusPage<Wp304EquipmentLookupItem>> EquipmentLookupAsync(
        Wp303EquipmentConsumeStatusLookupQuery query, CancellationToken token)
    {
        var keyword = OptionalText(query.Keyword, 100, "Keyword");
        var page = NormalizePage(query.Page, query.PageSize);
        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        await ExecuteAsync(connection, transaction, "SET TRANSACTION READ ONLY;", token);

        var rows = new List<Wp304EquipmentLookupItem>(page.PageSize + 1);
        await using (var command = new NpgsqlCommand("""
            SELECT equipmentno, equipmenttype, equipmentname
            FROM "YMES".tbleqpequipmentbasis
            WHERE issuestate = 2
              AND (@keyword IS NULL OR strpos(upper(equipmentno), upper(@keyword)) > 0
                   OR strpos(upper(equipmenttype), upper(@keyword)) > 0
                   OR strpos(upper(COALESCE(equipmentname, '')), upper(@keyword)) > 0)
            ORDER BY equipmenttype COLLATE "C", equipmentno COLLATE "C"
            LIMIT @take OFFSET @offset;
            """, connection, transaction))
        {
            command.Parameters.Add("keyword", NpgsqlDbType.Varchar).Value = (object?)keyword ?? DBNull.Value;
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
                rows.Add(new(reader.GetString(0), reader.GetString(1), NullableString(reader, 2)));
        }

        var result = FinishPage(page, rows);
        await transaction.CommitAsync(token);
        return result;
    }

    public async Task<Wp303EquipmentConsumeStatusPage<Wp303EquipmentConsumeBasisItem>> ConsumableLookupAsync(
        Wp303EquipmentConsumeBasisLookupQuery query, CancellationToken token)
    {
        var equipmentNo = RequiredText(query.EquipmentNo, 50, "EquipmentNo");
        var keyword = OptionalText(query.Keyword, 100, "Keyword");
        var page = NormalizePage(query.Page, query.PageSize);
        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        await ExecuteAsync(connection, transaction, "SET TRANSACTION READ ONLY;", token);
        var equipmentType = await GetEquipmentTypeAsync(connection, transaction, equipmentNo, token);

        var rows = new List<Wp303EquipmentConsumeBasisItem>(page.PageSize + 1);
        await using (var command = new NpgsqlCommand("""
            SELECT equipmenttype, consumeno, quantity, toleranceqty, period, toleranceperiod
            FROM "YMES".tbleqpconsumebasis
            WHERE equipmenttype = @equipmentType
              AND (@keyword IS NULL OR strpos(upper(consumeno), upper(@keyword)) > 0)
            ORDER BY consumeno COLLATE "C"
            LIMIT @take OFFSET @offset;
            """, connection, transaction))
        {
            Text(command, "equipmentType", equipmentType);
            command.Parameters.Add("keyword", NpgsqlDbType.Varchar).Value = (object?)keyword ?? DBNull.Value;
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
                rows.Add(new(reader.GetString(0), reader.GetString(1), reader.GetDecimal(2),
                    reader.GetDecimal(3), reader.GetDecimal(4), reader.GetDecimal(5)));
        }

        var result = FinishPage(page, rows);
        await transaction.CommitAsync(token);
        return result;
    }

    public async Task<Wp303EquipmentConsumeStatusItem> CreateAsync(
        Wp303EquipmentConsumeStatusAddRequest request, string actor, CancellationToken token)
    {
        var input = ValidateInput(request.EquipmentNo, request.ConsumeNo, request.ConsumeSeq,
            request.CurQty, request.CurPeriod);
        actor = RequiredText(actor, 10, "登入者");
        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        await LockConsumeNoAsync(connection, transaction, input.ConsumeNo, token);
        var equipmentType = await GetEquipmentTypeAsync(connection, transaction, input.EquipmentNo, token);
        await RequireConsumableAsync(connection, transaction, equipmentType, input.ConsumeNo, token);
        await EnsureSequenceNotLoggedAsync(connection, transaction, input.ConsumeNo, input.ConsumeSeq, token);
        var now = TaipeiNow();

        await using (var command = new NpgsqlCommand("""
            INSERT INTO "YMES".tblwipeqpconsumestatus
                (equipmentno, consumeno, consumeseq, curqty, curperiod, creator, createdate)
            VALUES (@equipmentNo, @consumeNo, @consumeSeq, @curQty, @curPeriod, @actor, @now);
            """, connection, transaction))
        {
            BindInput(command, input);
            Text(command, "actor", actor);
            Time(command, "now", now);
            if (await command.ExecuteNonQueryAsync(token) != 1)
                throw Unavailable("新增結果不明，請重新查詢核對。");
        }

        var result = new Wp303EquipmentConsumeStatusItem(input.EquipmentNo, input.ConsumeNo,
            input.ConsumeSeq, input.CurQty, input.CurPeriod, actor, now, null, null);
        await transaction.CommitAsync(token);
        return result;
    }

    public async Task<Wp303EquipmentConsumeStatusItem> UpdateAsync(
        Wp303EquipmentConsumeStatusEditRequest request, string actor, CancellationToken token)
    {
        var input = ValidateInput(request.EquipmentNo, request.ConsumeNo, request.ConsumeSeq,
            request.CurQty, request.CurPeriod);
        var original = ValidateExpected(request.EquipmentNo, request.ConsumeNo, request.OriginalConsumeSeq,
            request.OriginalCurQty, request.OriginalCurPeriod);
        if (!StringComparer.Ordinal.Equals(input.EquipmentNo, original.EquipmentNo)
            || !StringComparer.Ordinal.Equals(input.ConsumeNo, original.ConsumeNo))
            throw Bad("編輯不可變更 EquipmentNo 或 ConsumeNo。");
        actor = RequiredText(actor, 10, "登入者");
        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        await LockConsumeNoAsync(connection, transaction, input.ConsumeNo, token);
        var equipmentType = await GetEquipmentTypeAsync(connection, transaction, input.EquipmentNo, token);
        await RequireConsumableAsync(connection, transaction, equipmentType, input.ConsumeNo, token);
        var current = await ReadStatusAsync(connection, transaction, original.EquipmentNo,
            original.ConsumeNo, updateLock: true, token) ?? throw NotFound();
        EnsureExpected(current, original);
        await EnsureSequenceNotLoggedAsync(connection, transaction, input.ConsumeNo, input.ConsumeSeq, token);

        await using (var command = new NpgsqlCommand("""
            UPDATE "YMES".tblwipeqpconsumestatus
            SET consumeseq = @consumeSeq, curqty = @curQty, curperiod = @curPeriod
            WHERE equipmentno = @equipmentNo AND consumeno = @consumeNo
              AND consumeseq = @originalConsumeSeq
              AND curqty = @originalCurQty AND curperiod = @originalCurPeriod;
            """, connection, transaction))
        {
            BindInput(command, input);
            BindExpectedValues(command, original, "original");
            if (await command.ExecuteNonQueryAsync(token) != 1) throw Conflict("現況資料已變更，請重新查詢後再編輯。");
        }

        var result = current with
        {
            ConsumeSeq = input.ConsumeSeq,
            CurQty = input.CurQty,
            CurPeriod = input.CurPeriod
        };
        await transaction.CommitAsync(token);
        return result;
    }

    public async Task<Wp303EquipmentConsumeStatusDeleteResult> DeleteManyAsync(
        Wp303EquipmentConsumeStatusDeleteRequest request, CancellationToken token)
    {
        var items = request.Items;
        if (items is null || items.Count is < 1 or > MaxDeleteCount)
            throw Bad($"刪除項目須介於 1 到 {MaxDeleteCount} 筆。");

        var expected = items.Select(item => ValidateExpected(item.EquipmentNo, item.ConsumeNo,
                item.ConsumeSeq, item.CurQty, item.CurPeriod))
            .ToArray();
        if (expected.Select(item => (item.EquipmentNo, item.ConsumeNo)).Distinct().Count() != expected.Length)
            throw Bad("刪除清單不可重複指定相同設備與耗品。");

        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        foreach (var consumeNo in expected.Select(item => item.ConsumeNo).Distinct(StringComparer.Ordinal)
                     .OrderBy(item => item, StringComparer.Ordinal))
            await LockConsumeNoAsync(connection, transaction, consumeNo, token);

        foreach (var item in expected.OrderBy(item => item.EquipmentNo, StringComparer.Ordinal)
                     .ThenBy(item => item.ConsumeNo, StringComparer.Ordinal))
        {
            var current = await ReadStatusAsync(connection, transaction, item.EquipmentNo,
                item.ConsumeNo, updateLock: true, token) ?? throw NotFound();
            EnsureExpected(current, item);
        }

        foreach (var item in expected.OrderBy(item => item.EquipmentNo, StringComparer.Ordinal)
                     .ThenBy(item => item.ConsumeNo, StringComparer.Ordinal))
        {
            await using var command = new NpgsqlCommand("""
                DELETE FROM "YMES".tblwipeqpconsumestatus
                WHERE equipmentno = @equipmentNo AND consumeno = @consumeNo
                  AND consumeseq = @consumeSeq AND curqty = @curQty AND curperiod = @curPeriod;
                """, connection, transaction);
            BindExpected(command, item);
            if (await command.ExecuteNonQueryAsync(token) != 1) throw Conflict("現況資料已變更，整批刪除已取消。");
        }

        await transaction.CommitAsync(token);
        return new(expected.Length);
    }

    public async Task<Wp303EquipmentConsumeStatusItem> ResetAsync(
        Wp303EquipmentConsumeStatusResetRequest request, string actor, CancellationToken token)
    {
        var expected = ValidateExpected(request.EquipmentNo, request.ConsumeNo, request.ConsumeSeq,
            request.CurQty, request.CurPeriod);
        var newSequence = RequiredText(request.NewConsumeSeq, 30, "新 ConsumeSeq");
        if (string.Equals(newSequence, expected.ConsumeSeq, StringComparison.OrdinalIgnoreCase))
            throw Bad("新 ConsumeSeq 必須與目前序號不同。");
        actor = RequiredText(actor, 10, "登入者");

        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        await LockConsumeNoAsync(connection, transaction, expected.ConsumeNo, token);
        var equipmentType = await GetEquipmentTypeAsync(connection, transaction, expected.EquipmentNo, token);
        var basis = await RequireConsumableAsync(connection, transaction, equipmentType, expected.ConsumeNo, token);
        await EnsureSequenceNotLoggedAsync(connection, transaction, expected.ConsumeNo, newSequence, token);
        var current = await ReadStatusAsync(connection, transaction, expected.EquipmentNo,
            expected.ConsumeNo, updateLock: true, token) ?? throw NotFound();
        EnsureExpected(current, expected);

        // The native reset view clears both accumulated values once either
        // quantity or finite-period limit has been reached. Keep that behavior.
        var depleted = basis.Quantity - current.CurQty <= 0
            || (basis.Period != -1 && basis.Period - current.CurPeriod <= 0);
        var accQty = depleted ? 0m : current.CurQty;
        var accPeriod = depleted ? 0m : current.CurPeriod;
        var now = TaipeiNow();

        await using (var log = new NpgsqlCommand("""
            INSERT INTO "YMES".tblwipeqpconsumelog
                (equipmentno, consumeno, consumeseq, creator, createdate, accqty, accperiod)
            VALUES (@equipmentNo, @consumeNo, @oldConsumeSeq, @actor, @now, @accQty, @accPeriod);
            """, connection, transaction))
        {
            Text(log, "equipmentNo", expected.EquipmentNo);
            Text(log, "consumeNo", expected.ConsumeNo);
            Text(log, "oldConsumeSeq", current.ConsumeSeq);
            Text(log, "actor", actor);
            Time(log, "now", now);
            log.Parameters.Add("accQty", NpgsqlDbType.Numeric).Value = accQty;
            log.Parameters.Add("accPeriod", NpgsqlDbType.Numeric).Value = accPeriod;
            if (await log.ExecuteNonQueryAsync(token) != 1) throw Unavailable("耗品歷程未寫入，重置已取消。");
        }

        await using (var status = new NpgsqlCommand("""
            UPDATE "YMES".tblwipeqpconsumestatus
            SET consumeseq = @newConsumeSeq, curqty = 0, curperiod = 0,
                revisor = @actor, revisedate = @now
            WHERE equipmentno = @equipmentNo AND consumeno = @consumeNo
              AND consumeseq = @oldConsumeSeq
              AND curqty = @oldCurQty AND curperiod = @oldCurPeriod;
            """, connection, transaction))
        {
            Text(status, "equipmentNo", expected.EquipmentNo);
            Text(status, "consumeNo", expected.ConsumeNo);
            BindExpectedValues(status, expected, "old");
            Text(status, "newConsumeSeq", newSequence);
            Text(status, "actor", actor);
            Time(status, "now", now);
            if (await status.ExecuteNonQueryAsync(token) != 1) throw Conflict("現況資料已變更，重置已取消。");
        }

        var result = current with
        {
            ConsumeSeq = newSequence,
            CurQty = 0,
            CurPeriod = 0,
            Revisor = actor,
            ReviseDate = now
        };
        await transaction.CommitAsync(token);
        return result;
    }

    private static async Task<string> GetEquipmentTypeAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string equipmentNo, CancellationToken token)
    {
        await using var command = new NpgsqlCommand("""
            SELECT equipmenttype
            FROM "YMES".tbleqpequipmentbasis
            WHERE equipmentno = @equipmentNo AND issuestate = 2;
            """, connection, transaction);
        Text(command, "equipmentNo", equipmentNo);
        var result = await command.ExecuteScalarAsync(token);
        if (result is not string equipmentType || string.IsNullOrWhiteSpace(equipmentType))
            throw Bad("設備不存在、未啟用或沒有設備類別。");
        return equipmentType;
    }

    private static async Task<Wp303EquipmentConsumeBasisItem> RequireConsumableAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string equipmentType,
        string consumeNo, CancellationToken token)
    {
        await using var command = new NpgsqlCommand("""
            SELECT equipmenttype, consumeno, quantity, toleranceqty, period, toleranceperiod
            FROM "YMES".tbleqpconsumebasis
            WHERE equipmenttype = @equipmentType AND consumeno = @consumeNo;
            """, connection, transaction);
        Text(command, "equipmentType", equipmentType);
        Text(command, "consumeNo", consumeNo);
        await using var reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token)) throw Bad("此設備類別沒有對應的耗品基準。");
        return new(reader.GetString(0), reader.GetString(1), reader.GetDecimal(2),
            reader.GetDecimal(3), reader.GetDecimal(4), reader.GetDecimal(5));
    }

    private static async Task LockConsumeNoAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string consumeNo, CancellationToken token)
    {
        await using var command = new NpgsqlCommand("""
            SELECT pg_advisory_xact_lock(hashtextextended('YMES:WP303:ConsumeNo:' || @consumeNo, 0));
            """, connection, transaction);
        Text(command, "consumeNo", consumeNo);
        await command.ExecuteNonQueryAsync(token);
    }

    private static async Task EnsureSequenceNotLoggedAsync(NpgsqlConnection connection,
        NpgsqlTransaction transaction, string consumeNo, string consumeSeq, CancellationToken token)
    {
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1 FROM "YMES".tblwipeqpconsumelog
                WHERE consumeno = @consumeNo AND upper(consumeseq) = upper(@consumeSeq));
            """, connection, transaction);
        Text(command, "consumeNo", consumeNo);
        Text(command, "consumeSeq", consumeSeq);
        if (await command.ExecuteScalarAsync(token) is true) throw Conflict("相同 ConsumeNo 的歷程已存在此 ConsumeSeq。");
    }

    private static async Task<Wp303EquipmentConsumeStatusItem?> ReadStatusAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string equipmentNo,
        string consumeNo, bool updateLock, CancellationToken token)
    {
        await using var command = new NpgsqlCommand("""
            SELECT equipmentno, consumeno, consumeseq, curqty, curperiod,
                   creator, createdate, revisor, revisedate
            FROM "YMES".tblwipeqpconsumestatus
            WHERE equipmentno = @equipmentNo AND consumeno = @consumeNo
            """ + (updateLock ? " FOR UPDATE;" : ";"), connection, transaction);
        Text(command, "equipmentNo", equipmentNo);
        Text(command, "consumeNo", consumeNo);
        await using var reader = await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token) ? MapStatus(reader) : null;
    }

    private static void EnsureExpected(Wp303EquipmentConsumeStatusItem current, ExpectedInput expected)
    {
        if (!StringComparer.Ordinal.Equals(current.EquipmentNo, expected.EquipmentNo)
            || !StringComparer.Ordinal.Equals(current.ConsumeNo, expected.ConsumeNo)
            || !StringComparer.Ordinal.Equals(current.ConsumeSeq, expected.ConsumeSeq)
            || current.CurQty != expected.CurQty || current.CurPeriod != expected.CurPeriod)
            throw Conflict("現況資料已變更，請重新查詢後再操作。");
    }

    private static StatusInput ValidateInput(string? equipmentNo, string? consumeNo,
        string? consumeSeq, decimal? curQty, decimal? curPeriod)
    {
        var normalizedEquipmentNo = RequiredText(equipmentNo, 50, "EquipmentNo");
        var normalizedConsumeNo = RequiredText(consumeNo, 25, "ConsumeNo");
        var normalizedConsumeSeq = RequiredText(consumeSeq, 30, "ConsumeSeq");
        if (curQty is null || curQty < 0 || decimal.Truncate(curQty.Value) != curQty.Value || curQty > 99_999_999)
            throw Bad("CurQty 必須是 0 到 99,999,999 的整數。");
        if (curPeriod is null || curPeriod < -99_999.99m || curPeriod > 99_999.99m
            || decimal.Round(curPeriod.Value, 2) != curPeriod.Value)
            throw Bad("CurPeriod 超出 NUMBER(7,2) 範圍或超過兩位小數。");
        return new(normalizedEquipmentNo, normalizedConsumeNo, normalizedConsumeSeq, curQty.Value, curPeriod.Value);
    }

    private static ExpectedInput ValidateExpected(string? equipmentNo, string? consumeNo,
        string? consumeSeq, decimal? curQty, decimal? curPeriod)
    {
        var normalized = ValidateInput(equipmentNo, consumeNo, consumeSeq, curQty, curPeriod);
        return new(normalized.EquipmentNo, normalized.ConsumeNo, normalized.ConsumeSeq,
            normalized.CurQty, normalized.CurPeriod);
    }

    private static string RequiredText(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) throw Bad($"{field} 不可空白。");
        var normalized = value.Trim();
        try { _ = new UTF8Encoding(false, true).GetByteCount(normalized); }
        catch (EncoderFallbackException) { throw Bad($"{field} 含無效 Unicode。"); }
        if (normalized.Contains('\0') || normalized.Any(char.IsControl)
            || normalized.EnumerateRunes().Count() > maxLength)
            throw Bad($"{field} 長度或字元不正確（最多 {maxLength} 字元）。");
        return normalized;
    }

    private static string? OptionalText(string? value, int maxLength, string field) =>
        string.IsNullOrWhiteSpace(value) ? null : RequiredText(value, maxLength, field);

    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string sql, CancellationToken token)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(token);
    }

    private static void BindInput(NpgsqlCommand command, StatusInput input)
    {
        Text(command, "equipmentNo", input.EquipmentNo);
        Text(command, "consumeNo", input.ConsumeNo);
        Text(command, "consumeSeq", input.ConsumeSeq);
        Numeric(command, "curQty", input.CurQty);
        Numeric(command, "curPeriod", input.CurPeriod);
    }

    private static void BindExpected(NpgsqlCommand command, ExpectedInput item)
    {
        Text(command, "equipmentNo", item.EquipmentNo);
        Text(command, "consumeNo", item.ConsumeNo);
        Text(command, "consumeSeq", item.ConsumeSeq);
        Numeric(command, "curQty", item.CurQty);
        Numeric(command, "curPeriod", item.CurPeriod);
    }

    private static void BindExpectedValues(NpgsqlCommand command, ExpectedInput item, string prefix)
    {
        Text(command, $"{prefix}ConsumeSeq", item.ConsumeSeq);
        Numeric(command, $"{prefix}CurQty", item.CurQty);
        Numeric(command, $"{prefix}CurPeriod", item.CurPeriod);
    }

    private static void Text(NpgsqlCommand command, string name, string value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = value;

    private static void Numeric(NpgsqlCommand command, string name, decimal value) =>
        command.Parameters.Add(name, NpgsqlDbType.Numeric).Value = value;

    private static void Time(NpgsqlCommand command, string name, DateTime value) =>
        command.Parameters.Add(name, NpgsqlDbType.Timestamp).Value = value;

    private static DateTime TaipeiNow()
    {
        var local = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TaipeiTimeZone).DateTime;
        return new DateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, local.Second,
            DateTimeKind.Unspecified);
    }

    private static Wp303EquipmentConsumeStatusItem MapStatus(NpgsqlDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetDecimal(3), reader.GetDecimal(4),
        NullableString(reader, 5), NullableDateTime(reader, 6), NullableString(reader, 7), NullableDateTime(reader, 8));

    private static string? NullableString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static DateTime? NullableDateTime(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Unspecified);

    private static NormalizedPage NormalizePage(int? page, int? pageSize)
    {
        var normalizedPage = page ?? 1;
        var normalizedPageSize = pageSize ?? 50;
        if (normalizedPage is < 1 or > MaxPage) throw Bad($"頁碼必須介於 1 到 {MaxPage:N0}。");
        if (normalizedPageSize is < 1 or > MaxPageSize) throw Bad($"每頁筆數必須介於 1 到 {MaxPageSize}。");
        return new(normalizedPage, normalizedPageSize, checked((normalizedPage - 1) * normalizedPageSize));
    }

    private static Wp303EquipmentConsumeStatusPage<T> FinishPage<T>(NormalizedPage page, List<T> rows)
    {
        var hasMore = rows.Count > page.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        return new(page.Page, page.PageSize, hasMore, rows);
    }

    private static Wp303EquipmentConsumeStatusRuleException Bad(string message) => new(400, message);
    private static Wp303EquipmentConsumeStatusRuleException NotFound() => new(404, "找不到設備耗品現況。");
    private static Wp303EquipmentConsumeStatusRuleException Conflict(string message) => new(409, message);
    private static Wp303EquipmentConsumeStatusRuleException Unavailable(string message) => new(503, message);

    private sealed record NormalizedPage(int Page, int PageSize, int Offset);
    private sealed record StatusInput(string EquipmentNo, string ConsumeNo, string ConsumeSeq, decimal CurQty, decimal CurPeriod);
    private sealed record ExpectedInput(string EquipmentNo, string ConsumeNo, string ConsumeSeq, decimal CurQty, decimal CurPeriod);
}

public sealed class Wp303EquipmentConsumeStatusRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
