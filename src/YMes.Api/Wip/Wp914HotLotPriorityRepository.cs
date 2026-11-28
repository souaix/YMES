using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed class Wp914HotLotPriorityRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 10_000;
    private const int MaxPageSize = 100;
    private const int MaxSelectedLots = 200;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Wp914Page<Wp914LotItem>> GetLotsAsync(Wp914LotQuery query, CancellationToken token)
    {
        var roNo = OptionalText(query.RoNo, 25, "RONo");
        var moNo = OptionalText(query.MoNo, 50, "MONo");
        var productNo = OptionalText(query.ProductNo, 50, "ProductNo");
        var areaNo = OptionalText(query.AreaNo, 20, "AreaNo");
        var opNo = OptionalText(query.OpNo, 20, "OPNo");
        var lotNo = OptionalText(query.LotNo, 50, "LotNo");
        if (roNo is null && moNo is null && productNo is null && areaNo is null && opNo is null && lotNo is null)
            throw Bad(400, "請至少提供一個查詢條件。");
        var page = NormalizePage(query.Page, query.PageSize);

        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await BeginReadOnlyAsync(connection, token);
        var rows = new List<Wp914LotItem>(page.PageSize + 1);
        await using (var command = new NpgsqlCommand("""
            SELECT s.lotno,b.baselotno,b.rono,s.areano,b.mono,s.opno,b.productno,s.psno,s.processno,
                   COALESCE(o.opname,''),s.brno,COALESCE(s.opgroupno,'N/A'),
                   s.curqty,s.curunitno,s.sysqty,s.sysunitno,s.status,b.lotstate,
                   COALESCE(b.hotlot,0)::integer,COALESCE(b.priority,99)::integer,s.lotstamp
            FROM "YMES".tblwiplotstate s
            JOIN "YMES".tblwiplotbasis b ON b.baselotno=s.baselotno
            LEFT JOIN "YMES".tblopbasis o ON o.opno=s.opno
            WHERE b.lotstate=1 AND s.status BETWEEN 0 AND 5
              AND (@roNo IS NULL OR upper(b.rono)=@roNo)
              AND (@moNo IS NULL OR upper(b.mono)=@moNo)
              AND (@productNo IS NULL OR upper(b.productno)=@productNo)
              AND (@areaNo IS NULL OR upper(s.areano)=@areaNo)
              AND (@opNo IS NULL OR upper(s.opno)=@opNo)
              AND (@lotNo IS NULL OR upper(s.lotno)=@lotNo)
            ORDER BY COALESCE(b.hotlot,0) DESC,COALESCE(b.priority,99),s.lotno COLLATE "C",s.opno COLLATE "C",s.status
            LIMIT @take OFFSET @offset;
            """, connection, transaction))
        {
            AddText(command, "roNo", roNo);
            AddText(command, "moNo", moNo);
            AddText(command, "productNo", productNo);
            AddText(command, "areaNo", areaNo);
            AddText(command, "opNo", opNo);
            AddText(command, "lotNo", lotNo);
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                rows.Add(new Wp914LotItem(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7),
                    reader.GetString(8), reader.GetString(9), reader.GetString(10), reader.GetString(11),
                    reader.GetDecimal(12), reader.GetString(13), reader.GetDecimal(14), reader.GetString(15),
                    checked((int)reader.GetDecimal(16)), checked((int)reader.GetDecimal(17)),
                    reader.GetInt32(18), reader.GetInt32(19), reader.GetDecimal(20)));
            }
        }
        var result = FinishPage(page, rows);
        await transaction.CommitAsync(token);
        return result;
    }

    public async Task<Wp914Page<string>> GetLookupAsync(
        string field, Wp914LookupQuery query, CancellationToken token)
    {
        var keyword = RequiredText(query.Keyword, 100, "Keyword");
        var page = NormalizePage(query.Page, query.PageSize);
        var expression = field.Trim().ToLowerInvariant() switch
        {
            "rono" => "b.rono",
            "productno" => "b.productno",
            "areano" => "s.areano",
            "opno" => "s.opno",
            _ => throw Bad(400, "Lookup field 僅支援 roNo、productNo、areaNo、opNo。")
        };

        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await BeginReadOnlyAsync(connection, token);
        var values = new List<string>(page.PageSize + 1);
        await using (var command = new NpgsqlCommand($"""
            SELECT lookup.value
            FROM (
                SELECT DISTINCT {expression} AS value
                FROM "YMES".tblwiplotstate s
                JOIN "YMES".tblwiplotbasis b ON b.baselotno=s.baselotno
                WHERE b.lotstate=1 AND s.status BETWEEN 0 AND 5
                  AND {expression} IS NOT NULL
                  AND strpos(upper({expression}),upper(@keyword))>0
            ) lookup
            ORDER BY lookup.value COLLATE "C"
            LIMIT @take OFFSET @offset;
            """, connection, transaction))
        {
            AddText(command, "keyword", keyword);
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token)) values.Add(reader.GetString(0));
        }
        var result = FinishPage(page, values);
        await transaction.CommitAsync(token);
        return result;
    }

    public async Task<Wp914SetResult> SetAsync(
        string? idempotencyKey, Wp914SetRequest request, string actor, CancellationToken token)
    {
        var requestId = ParseRequestId(idempotencyKey);
        actor = RequiredText(actor, 10, "登入者");
        if (request.HotLot is not (0 or 1)) throw Bad(400, "HotLot 必須明確選擇 0 或 1。");
        if (request.Priority is null or < 0 or > 99) throw Bad(400, "Priority 必須是 0–99 的整數。");
        if (request.Lots is null || request.Lots.Count is < 1 or > MaxSelectedLots)
            throw Bad(400, $"請選取 1–{MaxSelectedLots} 筆生產批。");

        var selected = request.Lots.Select(item =>
        {
            if (item is null) throw Bad(400, "Lot 清單不可包含空值。");
            var lotNo = RequiredKey(item.LotNo, 50, "LotNo");
            var baseLotNo = RequiredKey(item.BaseLotNo, 50, "BaseLotNo");
            var opNo = RequiredKey(item.OpNo, 20, "OPNo");
            var status = item.Status;
            var version = item.Version;
            if (status is null or < 0 or > 5) throw Bad(400, "Lot Status 必須是 0–5。");
            if (version is null or < 0 or > 99_999_999_999m) throw Bad(400, "Lot 版本格式錯誤，請重新查詢。");
            return new SelectedLot(lotNo, baseLotNo, opNo, status.Value, version.Value);
        }).ToArray();
        if (selected.Select(item => (item.LotNo, item.OpNo, item.Status)).Distinct().Count() != selected.Length)
            throw Bad(400, "同一 LotNo／OPNo／Status 列不可重複選取。");

        var ordered = selected.OrderBy(item => item.LotNo, StringComparer.Ordinal)
            .ThenBy(item => item.OpNo, StringComparer.Ordinal).ThenBy(item => item.Status).ToArray();
        var payloadHash = HashPayload(request.HotLot.Value, request.Priority.Value, ordered);

        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        await using (var command = new NpgsqlCommand("""
            SELECT pg_advisory_xact_lock(hashtextextended('YMES:WP914:' || @requestId::text,0));
            """, connection, transaction))
        {
            command.Parameters.Add("requestId", NpgsqlDbType.Uuid).Value = requestId;
            await command.ExecuteNonQueryAsync(token);
        }

        await using (var command = new NpgsqlCommand("""
            SELECT payloadsha256,resultjson::text
            FROM "YMES".ym_wp914_request
            WHERE requestid=@requestId;
            """, connection, transaction))
        {
            command.Parameters.Add("requestId", NpgsqlDbType.Uuid).Value = requestId;
            await using var reader = await command.ExecuteReaderAsync(token);
            if (await reader.ReadAsync(token))
            {
                var previousHash = reader.GetString(0);
                var previousJson = reader.GetString(1);
                await reader.DisposeAsync();
                if (!CryptographicOperations.FixedTimeEquals(
                        System.Text.Encoding.ASCII.GetBytes(previousHash),
                        System.Text.Encoding.ASCII.GetBytes(payloadHash)))
                    throw Bad(409, "Idempotency-Key 已用於不同內容；請重新查詢後再提交。");
                var replay = JsonSerializer.Deserialize<Wp914SetResult>(previousJson, JsonOptions);
                if (replay is null) throw Bad(503, "既有請求結果無法讀取；請保留 Idempotency-Key 並聯絡管理者。");
                await transaction.CommitAsync(token);
                return replay with { Replayed = true };
            }
        }

        var lockedRows = await LockSelectedLotsAsync(connection, transaction, ordered, token);
        if (lockedRows.Count != ordered.Length)
            throw Bad(409, "所選生產批狀態或版本已變更，請重新 Query 後再提交。");

        var baseLotNos = ordered.Select(item => item.BaseLotNo).Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var updates = new List<Wp914BaseUpdate>(baseLotNos.Length);
        foreach (var baseLotNo in baseLotNos)
        {
            await using (var command = new NpgsqlCommand("""
                UPDATE "YMES".tblwiplotbasis
                SET hotlot=@hotLot,priority=@priority
                WHERE baselotno=@baseLotNo AND lotstate=1;
                """, connection, transaction))
            {
                command.Parameters.Add("hotLot", NpgsqlDbType.Numeric).Value = (decimal)request.HotLot.Value;
                command.Parameters.Add("priority", NpgsqlDbType.Numeric).Value = (decimal)request.Priority.Value;
                AddText(command, "baseLotNo", baseLotNo);
                if (await command.ExecuteNonQueryAsync(token) != 1)
                    throw Bad(409, "基礎生產批已變更或不再有效，整批更新已取消。");
            }

            long affectedLotCount;
            await using (var command = new NpgsqlCommand("""
                SELECT count(DISTINCT lotno)
                FROM "YMES".tblwiplotstate
                WHERE baselotno=@baseLotNo;
                """, connection, transaction))
            {
                AddText(command, "baseLotNo", baseLotNo);
                affectedLotCount = Convert.ToInt64(await command.ExecuteScalarAsync(token), System.Globalization.CultureInfo.InvariantCulture);
            }
            updates.Add(new Wp914BaseUpdate(baseLotNo,
                ordered.Where(item => item.BaseLotNo == baseLotNo).Select(item => item.LotNo)
                    .Distinct(StringComparer.Ordinal).ToArray(),
                affectedLotCount, request.HotLot.Value, request.Priority.Value));
        }

        var result = new Wp914SetResult(requestId, DateTime.UtcNow, actor, updates);
        await using (var command = new NpgsqlCommand("""
            INSERT INTO "YMES".ym_wp914_request(requestid,actor,payloadsha256,resultjson,createdate)
            VALUES(@requestId,@actor,@payloadHash,CAST(@resultJson AS jsonb),@createdAt);
            """, connection, transaction))
        {
            command.Parameters.Add("requestId", NpgsqlDbType.Uuid).Value = requestId;
            AddText(command, "actor", actor);
            AddText(command, "payloadHash", payloadHash, 64);
            AddText(command, "resultJson", JsonSerializer.Serialize(result, JsonOptions));
            command.Parameters.Add("createdAt", NpgsqlDbType.TimestampTz).Value = result.UpdatedAtUtc;
            await command.ExecuteNonQueryAsync(token);
        }
        await transaction.CommitAsync(token);
        return result;
    }

    private static async Task<List<SelectedLot>> LockSelectedLotsAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction,
        IReadOnlyList<SelectedLot> selected, CancellationToken token)
    {
        await using var command = new NpgsqlCommand("""
            SELECT lotno,baselotno,opno,status,lotstamp
            FROM "YMES".ym_wp914_lock_selected_lots(
                @lotNos,@baseLotNos,@opNos,@statuses,@versions);
            """, connection, transaction);
        command.Parameters.Add("lotNos", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = selected.Select(item => item.LotNo).ToArray();
        command.Parameters.Add("baseLotNos", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = selected.Select(item => item.BaseLotNo).ToArray();
        command.Parameters.Add("opNos", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = selected.Select(item => item.OpNo).ToArray();
        command.Parameters.Add("statuses", NpgsqlDbType.Array | NpgsqlDbType.Numeric).Value = selected.Select(item => (decimal)item.Status).ToArray();
        command.Parameters.Add("versions", NpgsqlDbType.Array | NpgsqlDbType.Numeric).Value = selected.Select(item => item.Version).ToArray();
        var found = new List<SelectedLot>(selected.Count);
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
            found.Add(new SelectedLot(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                checked((int)reader.GetDecimal(3)), reader.GetDecimal(4)));
        return found;
    }

    private static async Task<NpgsqlTransaction> BeginReadOnlyAsync(
        NpgsqlConnection connection, CancellationToken token)
    {
        var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        await using var command = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction);
        await command.ExecuteNonQueryAsync(token);
        return transaction;
    }

    private static Wp914Page<T> FinishPage<T>(Page page, List<T> rows)
    {
        var hasMore = rows.Count > page.PageSize;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        return new Wp914Page<T>(page.Number, page.PageSize, hasMore, rows);
    }

    private static Page NormalizePage(int number, int size)
    {
        if (number is < 1 or > MaxPage) throw Bad(400, $"Page 必須介於 1–{MaxPage}。");
        if (size is < 1 or > MaxPageSize) throw Bad(400, $"PageSize 必須介於 1–{MaxPageSize}。");
        return new Page(number, size, checked((number - 1) * size));
    }

    private static string? OptionalText(string? value, int maxLength, string field)
    {
        if (value is null) return null;
        if (value.Contains('\0')) throw Bad(400, $"{field} 包含無效字元。");
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length == 0) return null;
        if (normalized.Length > maxLength) throw Bad(400, $"{field} 不可超過 {maxLength} 個字元。");
        return normalized;
    }

    private static string RequiredText(string? value, int maxLength, string field)
    {
        var normalized = OptionalText(value, maxLength, field);
        if (normalized is null) throw Bad(400, $"{field} 為必填。");
        return normalized;
    }

    private static string RequiredKey(string? value, int maxLength, string field)
    {
        if (value is null) throw Bad(400, $"{field} 為必填。");
        if (value.Contains('\0')) throw Bad(400, $"{field} 包含無效字元。");
        var key = value.Trim();
        if (key.Length == 0) throw Bad(400, $"{field} 為必填。");
        if (key.Length > maxLength) throw Bad(400, $"{field} 不可超過 {maxLength} 個字元。");
        return key;
    }

    private static Guid ParseRequestId(string? value)
    {
        if (!Guid.TryParse(value, out var id)) throw Bad(400, "請提供有效的 Idempotency-Key UUID。");
        return id;
    }

    private static string HashPayload(int hotLot, int priority, IReadOnlyList<SelectedLot> selected)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            hotLot,
            priority,
            lots = selected.Select(item => new
            {
                item.LotNo,
                item.BaseLotNo,
                item.OpNo,
                item.Status,
                item.Version
            })
        }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static void AddText(NpgsqlCommand command, string name, string? value, int? length = null)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Varchar);
        parameter.Value = value is null ? DBNull.Value : value;
        if (length is not null) parameter.Size = length.Value;
    }

    private static Wp914RuleException Bad(int statusCode, string message) => new(statusCode, message);

    private sealed record SelectedLot(string LotNo, string BaseLotNo, string OpNo, int Status, decimal Version);
    private sealed record Page(int Number, int PageSize, int Offset);
}
