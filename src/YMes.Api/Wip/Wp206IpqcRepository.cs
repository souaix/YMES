using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed class Wp206IpqcRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 10_000;
    private const int MaxPageSize = 100;
    private const int MaxAbnormalRows = 2_000;
    private const int MaxWriteRows = 100;
    private static readonly JsonSerializerOptions HashOptions = new(JsonSerializerDefaults.Web);

    public async Task<Wp206Page<Wp206OperationItem>> GetOperationsAsync(
        Wp206OperationQuery query, CancellationToken token)
    {
        var keyword = OptionalText(query.Keyword, 100, "Keyword");
        var page = NormalizePage(query.Page, query.PageSize);
        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await BeginReadOnlyAsync(connection, token);
        var rows = new List<Wp206OperationItem>(page.PageSize + 1);
        await using (var command = new NpgsqlCommand("""
            SELECT opno,opname
            FROM "YMES".tblopbasis
            WHERE opclass=1 AND issuestate=2
              AND (@keyword IS NULL OR strpos(upper(opno),upper(@keyword))>0
                   OR strpos(upper(opname),upper(@keyword))>0)
            ORDER BY opno COLLATE "C"
            LIMIT @take OFFSET @offset;
            """, connection, transaction))
        {
            Text(command, "keyword", keyword);
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = page.PageSize + 1;
            command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = page.Offset;
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token)) rows.Add(new(reader.GetString(0), reader.GetString(1)));
        }
        var result = FinishPage(page, rows);
        await transaction.CommitAsync(token);
        return result;
    }

    public async Task<IReadOnlyList<Wp206AbnormalItem>> GetAbnormalitiesAsync(string opNo, CancellationToken token)
    {
        opNo = RequiredText(opNo, 20, "OPNo");
        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await BeginReadOnlyAsync(connection, token);
        await RequireOperationAsync(connection, transaction, opNo, lockRow: false, token);
        var items = new List<Wp206AbnormalItem>();
        var byAbnormal = new Dictionary<string, AbnormalBuilder>(StringComparer.Ordinal);
        var rowCount = 0;
        await using (var command = new NpgsqlCommand("""
            SELECT b.abnormalno,b.abnormalname,b.abnormaltype,b.linkname,b.priority,p.position
            FROM "YMES".tblopabnormal a
            JOIN "YMES".tblqcabnormalbasis b ON b.abnormalno=a.abnormalno
            LEFT JOIN "YMES".tblqcabnormalposition p ON p.abnormalno=b.abnormalno
            WHERE a.opno=@opNo
            ORDER BY b.abnormalname COLLATE "C",b.abnormalno COLLATE "C",p.position COLLATE "C"
            LIMIT @take;
            """, connection, transaction))
        {
            Text(command, "opNo", opNo, 20);
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxAbnormalRows + 1;
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                rowCount++;
                if (rowCount > MaxAbnormalRows)
                    throw Bad(413, $"此作業站關聯異常／位置超過 {MaxAbnormalRows:N0} 筆，請縮小來源設定。");
                var abnormalNo = reader.GetString(0);
                if (!byAbnormal.TryGetValue(abnormalNo, out var item))
                {
                    item = new AbnormalBuilder(abnormalNo, reader.GetString(1), reader.GetString(2),
                        reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetDecimal(4));
                    byAbnormal.Add(abnormalNo, item);
                    items.Add(new Wp206AbnormalItem(item.AbnormalNo, item.AbnormalName, item.AbnormalType,
                        item.LinkName, item.Priority, item.Positions));
                }
                if (!reader.IsDBNull(5)) item.Positions.Add(reader.GetString(5));
            }
        }
        await transaction.CommitAsync(token);
        return items.Select(item => item with { Positions = item.Positions.ToArray() }).ToArray();
    }

    public async Task<Wp206RecordResult> CreateAsync(
        string? idempotencyKey, Wp206RecordRequest request, string actor, CancellationToken token)
    {
        var requestId = ParseRequestId(idempotencyKey);
        var lotNo = RequiredText(request.LotNo, 50, "LotNo");
        var opNo = RequiredText(request.OpNo, 20, "OPNo");
        actor = RequiredText(actor, 10, "登入者");
        var abnormalities = NormalizeSelections(request.Abnormalities);
        if (abnormalities.Sum(item => Math.Max(1, item.Positions.Count)) > MaxWriteRows)
            throw Bad(413, $"單次最多可新增 {MaxWriteRows} 筆異常記錄。");

        var payload = JsonSerializer.Serialize(new
        {
            lotNo,
            opNo,
            abnormalities = abnormalities.Select(item => new
            {
                item.AbnormalNo,
                positions = item.Positions.OrderBy(position => position, StringComparer.Ordinal).ToArray()
            }).OrderBy(item => item.AbnormalNo, StringComparer.Ordinal).ToArray()
        }, HashOptions);
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

        await using var connection = await dataSource.OpenConnectionAsync(token);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        await using (var timeout = new NpgsqlCommand("SET LOCAL statement_timeout='20s';", connection, transaction))
            await timeout.ExecuteNonQueryAsync(token);
        await using (var lockCommand = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(hashtextextended(@requestId, 206));", connection, transaction))
        {
            Text(lockCommand, "requestId", requestId.ToString("D"));
            await lockCommand.ExecuteNonQueryAsync(token);
        }

        var replay = await FindIdempotencyAsync(connection, transaction, requestId, token);
        if (replay is not null)
        {
            if (!StringComparer.Ordinal.Equals(replay.PayloadHash, payloadHash)
                || !StringComparer.Ordinal.Equals(replay.Creator, actor))
                throw Bad(409, "此 Idempotency-Key 已用於不同內容或不同登入者；未重複寫入。");
            await transaction.CommitAsync(token);
            return new Wp206RecordResult(replay.InsertedCount, true);
        }

        await RequireOperationAsync(connection, transaction, opNo, lockRow: true, token);
        await RequireLotAtOperationAsync(connection, transaction, lotNo, opNo, token);
        var abnormalNames = await ReadLinkedAbnormalitiesAsync(connection, transaction, opNo, abnormalities, token);
        await ValidatePositionsAsync(connection, transaction, abnormalities, abnormalNames, token);

        var insertedCount = 0;
        foreach (var item in abnormalities)
        {
            var positions = abnormalNames[item.AbnormalNo].Positions;
            var selectedPositions = positions.Count == 0 ? new string?[] { null } : item.Positions.Cast<string?>().ToArray();
            foreach (var position in selectedPositions)
            {
                await using var insert = new NpgsqlCommand("""
                    INSERT INTO "YMES".tblwipcont_abnormal(lotserial,opno,abnormalno,position)
                    VALUES(@lotNo,@opNo,@abnormalNo,@position);
                    """, connection, transaction);
                Text(insert, "lotNo", lotNo, 50);
                Text(insert, "opNo", opNo, 20);
                Text(insert, "abnormalNo", item.AbnormalNo, 20);
                Text(insert, "position", position, 50);
                await insert.ExecuteNonQueryAsync(token);
                insertedCount++;
            }
        }

        var createdAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        await using (var saveKey = new NpgsqlCommand("""
            INSERT INTO "YMES".ym_wp206_request(requestid,creator,payloadsha256,insertedcount,createdate)
            VALUES(@requestId,@actor,@hash,@insertedCount,@createdAt);
            """, connection, transaction))
        {
            saveKey.Parameters.Add("requestId", NpgsqlDbType.Uuid).Value = requestId;
            Text(saveKey, "actor", actor, 10);
            Text(saveKey, "hash", payloadHash, 64);
            saveKey.Parameters.Add("insertedCount", NpgsqlDbType.Integer).Value = insertedCount;
            saveKey.Parameters.Add("createdAt", NpgsqlDbType.Timestamp).Value = createdAt;
            await saveKey.ExecuteNonQueryAsync(token);
        }

        await transaction.CommitAsync(token);
        return new Wp206RecordResult(insertedCount, false);
    }

    private static async Task RequireOperationAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string opNo,
        bool lockRow,
        CancellationToken token)
    {
        var lockClause = lockRow ? "FOR SHARE" : "";
        await using var command = new NpgsqlCommand($"""
            SELECT opno FROM "YMES".tblopbasis
            WHERE opno=@opNo AND opclass=1 AND issuestate=2
            LIMIT 1 {lockClause};
            """, connection, transaction);
        Text(command, "opNo", opNo, 20);
        if (await command.ExecuteScalarAsync(token) is null)
            throw Bad(404, "找不到已核准的 IPQC 作業站。");
    }

    private static async Task RequireLotAtOperationAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction,
        string lotNo, string opNo, CancellationToken token)
    {
        await using var command = new NpgsqlCommand("""
            SELECT lotno FROM "YMES".tblwiplotstate
            WHERE lotno=@lotNo AND opno=@opNo
            ORDER BY status DESC
            LIMIT 1 FOR SHARE;
            """, connection, transaction);
        Text(command, "lotNo", lotNo, 50);
        Text(command, "opNo", opNo, 20);
        if (await command.ExecuteScalarAsync(token) is null)
            throw Bad(404, "找不到此作業站的生產批狀態；請確認 Lot No 與所選 OP No。");
    }

    private static async Task<Dictionary<string, AbnormalSnapshot>> ReadLinkedAbnormalitiesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string opNo,
        IReadOnlyList<NormalizedSelection> selections,
        CancellationToken token)
    {
        var requested = selections.Select(item => item.AbnormalNo).ToArray();
        var result = new Dictionary<string, AbnormalSnapshot>(StringComparer.Ordinal);
        await using var command = new NpgsqlCommand("""
            SELECT b.abnormalno,b.abnormalname
            FROM "YMES".tblopabnormal a
            JOIN "YMES".tblqcabnormalbasis b ON b.abnormalno=a.abnormalno
            WHERE a.opno=@opNo AND b.abnormalno=ANY(@abnormalNos)
            ORDER BY b.abnormalno COLLATE "C"
            FOR SHARE OF a,b;
            """, connection, transaction);
        Text(command, "opNo", opNo, 20);
        command.Parameters.Add("abnormalNos", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = requested;
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
            result[reader.GetString(0)] = new AbnormalSnapshot(reader.GetString(1), []);
        if (result.Count != requested.Length)
            throw Bad(400, "所選異常項目不存在或未關聯至此作業站。");
        return result;
    }

    private static async Task ValidatePositionsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<NormalizedSelection> selections,
        Dictionary<string, AbnormalSnapshot> abnormalNames,
        CancellationToken token)
    {
        var abnormalNos = selections.Select(item => item.AbnormalNo).ToArray();
        var positionsByAbnormal = abnormalNos.ToDictionary(item => item, _ => new List<string>(), StringComparer.Ordinal);
        await using (var command = new NpgsqlCommand("""
            SELECT abnormalno,position
            FROM "YMES".tblqcabnormalposition
            WHERE abnormalno=ANY(@abnormalNos)
            ORDER BY abnormalno COLLATE "C",position COLLATE "C"
            FOR SHARE;
            """, connection, transaction))
        {
            command.Parameters.Add("abnormalNos", NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = abnormalNos;
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
                positionsByAbnormal[reader.GetString(0)].Add(reader.GetString(1));
        }

        foreach (var item in selections)
        {
            var available = positionsByAbnormal[item.AbnormalNo];
            abnormalNames[item.AbnormalNo] = abnormalNames[item.AbnormalNo] with { Positions = available };
            if (available.Count == 0 && item.Positions.Count > 0)
                throw Bad(400, $"異常項目 {item.AbnormalNo} 沒有位置設定，不可傳入 Position。");
            if (available.Count > 0 && item.Positions.Count == 0)
                throw Bad(400, $"異常項目 {item.AbnormalNo} 有位置選項，至少要選擇一個位置。");
            if (item.Positions.Any(position => !available.Contains(position, StringComparer.Ordinal)))
                throw Bad(400, $"異常項目 {item.AbnormalNo} 包含不屬於該異常的 Position。");
        }
    }

    private static async Task<IdempotencyEntry?> FindIdempotencyAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, Guid requestId, CancellationToken token)
    {
        await using var command = new NpgsqlCommand("""
            SELECT creator,payloadsha256,insertedcount
            FROM "YMES".ym_wp206_request
            WHERE requestid=@requestId;
            """, connection, transaction);
        command.Parameters.Add("requestId", NpgsqlDbType.Uuid).Value = requestId;
        await using var reader = await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token)
            ? new IdempotencyEntry(reader.GetString(0), reader.GetString(1), reader.GetInt32(2))
            : null;
    }

    private static IReadOnlyList<NormalizedSelection> NormalizeSelections(
        IReadOnlyList<Wp206AbnormalSelection?>? items)
    {
        if (items is null || items.Count == 0) throw Bad(400, "至少要選擇一個異常項目。");
        if (items.Count > MaxWriteRows) throw Bad(413, $"單次最多可選擇 {MaxWriteRows} 個異常項目。");
        var seenAbnormal = new HashSet<string>(StringComparer.Ordinal);
        var normalized = new List<NormalizedSelection>(items.Count);
        foreach (var item in items)
        {
            if (item is null) throw Bad(400, "異常項目不可為空值。");
            var abnormalNo = RequiredText(item.AbnormalNo, 20, "AbnormalNo");
            if (!seenAbnormal.Add(abnormalNo)) throw Bad(400, $"異常項目 {abnormalNo} 重複。");
            var positions = (item.Positions ?? Array.Empty<string?>())
                .Select(position => RequiredText(position, 50, "Position"))
                .ToArray();
            if (positions.Distinct(StringComparer.Ordinal).Count() != positions.Length)
                throw Bad(400, $"異常項目 {abnormalNo} 的 Position 不可重複。");
            normalized.Add(new NormalizedSelection(abnormalNo, positions));
        }
        return normalized;
    }

    private static async Task<NpgsqlTransaction> BeginReadOnlyAsync(NpgsqlConnection connection, CancellationToken token)
    {
        var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        await using var command = new NpgsqlCommand("SET TRANSACTION READ ONLY; SET LOCAL statement_timeout='20s';", connection, transaction);
        await command.ExecuteNonQueryAsync(token);
        return transaction;
    }

    private static Guid ParseRequestId(string? value)
    {
        if (!Guid.TryParseExact(value, "D", out var requestId) || requestId == Guid.Empty)
            throw Bad(400, "Idempotency-Key 必須是非空 UUID，供安全重送同一筆 IPQC 操作。");
        return requestId;
    }

    private static string RequiredText(string? value, int maxLength, string field) =>
        OptionalText(value, maxLength, field) ?? throw Bad(400, $"{field} 為必填欄位。");

    private static string? OptionalText(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        try { _ = new UTF8Encoding(false, true).GetByteCount(normalized); }
        catch (EncoderFallbackException) { throw Bad(400, $"{field} 含無效 Unicode。"); }
        if (normalized.Contains('\0') || normalized.Any(char.IsControl)
            || normalized.EnumerateRunes().Count() > maxLength)
            throw Bad(400, $"{field} 長度或字元不正確（最多 {maxLength} 字元）。");
        return normalized;
    }

    private static NormalizedPage NormalizePage(int? page, int? pageSize)
    {
        var currentPage = page ?? 1;
        var size = pageSize ?? 50;
        if (currentPage is < 1 or > MaxPage) throw Bad(400, $"頁碼必須介於 1 到 {MaxPage:N0}。");
        if (size is < 1 or > MaxPageSize) throw Bad(400, $"每頁筆數必須介於 1 到 {MaxPageSize}。");
        return new(currentPage, size, checked((currentPage - 1) * size));
    }

    private static Wp206Page<T> FinishPage<T>(NormalizedPage page, List<T> rows)
    {
        var more = rows.Count > page.PageSize;
        if (more) rows.RemoveAt(rows.Count - 1);
        return new(page.Page, page.PageSize, more, rows);
    }

    private static void Text(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static void Text(NpgsqlCommand command, string name, string? value, int maxLength) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, maxLength).Value = (object?)value ?? DBNull.Value;

    private static Wp206IpqcRuleException Bad(int status, string message) => new(status, message);

    private sealed record NormalizedPage(int Page, int PageSize, int Offset);
    private sealed record NormalizedSelection(string AbnormalNo, IReadOnlyList<string> Positions);
    private sealed record AbnormalSnapshot(string Name, List<string> Positions);
    private sealed record IdempotencyEntry(string Creator, string PayloadHash, int InsertedCount);
    private sealed class AbnormalBuilder(string abnormalNo, string abnormalName, string abnormalType,
        string linkName, decimal? priority)
    {
        public string AbnormalNo { get; } = abnormalNo;
        public string AbnormalName { get; } = abnormalName;
        public string AbnormalType { get; } = abnormalType;
        public string LinkName { get; } = linkName;
        public decimal? Priority { get; } = priority;
        public List<string> Positions { get; } = [];
    }
}
