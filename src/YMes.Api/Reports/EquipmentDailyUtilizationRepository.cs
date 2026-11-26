using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Reports;

public sealed class EquipmentDailyUtilizationRepository(NpgsqlDataSource dataSource)
{
    private const int MaxRangeDays = 31;
    private const int MaxListValues = 100;
    private const int MaxEquipmentRows = 2_000;
    private static readonly TimeZoneInfo TaipeiTimeZone = FindTaipeiTimeZone();

    private const string CandidateSql = """
        WITH parents AS (
            SELECT e.equipmentno, e.equipmenttype, e.equipmentname,
                   NULLIF(e.belongequipmentno, '') AS belongequipmentno,
                   COALESCE(area.containareano, area.baseareano) AS areano
            FROM "YMES".tbleqpequipmentbasis e
            LEFT JOIN LATERAL (
                SELECT r.containareano, r.baseareano
                FROM "YMES".tblsmdarearelation r
                WHERE r.objecttype = 2 AND r.objectno = e.equipmentno
                ORDER BY r.containareano COLLATE "C", r.baseareano COLLATE "C"
                LIMIT 1
            ) area ON TRUE
            WHERE e.issuestate = 2 AND COALESCE(e.chamberflag, 0) = 0
              AND (cardinality(@equipmentNos) = 0 OR upper(e.equipmentno) = ANY(@equipmentNos))
              AND (cardinality(@areaNos) = 0 OR EXISTS (
                  SELECT 1 FROM "YMES".tblsmdarearelation ar
                  WHERE ar.objecttype = 2 AND ar.objectno = e.equipmentno
                    AND (upper(ar.containareano) = ANY(@areaNos)
                         OR upper(ar.baseareano) = ANY(@areaNos))
              ))
            ORDER BY e.equipmentno COLLATE "C"
            LIMIT @take
        ), combined AS (
            SELECT p.equipmentno, p.equipmenttype, p.equipmentname, p.areano,
                   p.belongequipmentno, FALSE AS ischamber, p.equipmentno AS rootequipmentno
            FROM parents p
            UNION ALL
            SELECT c.equipmentno, c.equipmenttype, c.equipmentname,
                   COALESCE(area.containareano, area.baseareano) AS areano,
                   NULLIF(c.belongequipmentno, '') AS belongequipmentno,
                   TRUE AS ischamber, p.equipmentno AS rootequipmentno
            FROM parents p
            JOIN "YMES".tbleqpequipmentbasis c
              ON c.belongequipmentno = p.equipmentno
             AND COALESCE(c.chamberflag, 0) = 1
             AND c.issuestate = 2
            LEFT JOIN LATERAL (
                SELECT r.containareano, r.baseareano
                FROM "YMES".tblsmdarearelation r
                WHERE r.objecttype = 2 AND r.objectno = c.equipmentno
                ORDER BY r.containareano COLLATE "C", r.baseareano COLLATE "C"
                LIMIT 1
            ) area ON TRUE
            WHERE @chamberManage
        )
        SELECT equipmentno, equipmenttype, equipmentname, areano, belongequipmentno,
               ischamber, rootequipmentno
        FROM combined
        ORDER BY rootequipmentno COLLATE "C", ischamber, equipmentno COLLATE "C"
        LIMIT @take;
        """;

    private const string StateSql = """
        SELECT equipmentstate::int, statetype::int, statename, utilizationtype::int,
               statecolor::bigint, COALESCE(NULLIF(statecolor_hex, ''), '')
        FROM "YMES".tbleqpstatebasis
        ORDER BY equipmentstate;
        """;

    private const string StateSecondsSql = """
        WITH bounds AS (
            SELECT @rangeStart::timestamp without time zone AS range_start,
                   @rangeEnd::timestamp without time zone AS range_end,
                   timezone('Asia/Taipei', now()) AS now_local
        ), event_source AS (
            SELECT l.equipmentno, l.equipmentstate::int AS equipmentstate,
                   l.starttime, l.endtime, FALSE AS ischamber
            FROM "YMES".tblemsequipmentstatelog l
            CROSS JOIN bounds b
            WHERE l.equipmentno = ANY(@mainEquipmentNos)
              AND l.starttime < b.range_end
              AND l.endtime > b.range_start
              AND l.endtime <= b.range_end
            UNION ALL
            SELECT l.equipmentno, l.equipmentstate::int AS equipmentstate,
                   l.starttime, l.endtime, TRUE AS ischamber
            FROM "YMES".tblemsequipmentstatelog l
            CROSS JOIN bounds b
            WHERE l.equipmentno = ANY(@chamberEquipmentNos)
              AND l.starttime < b.range_end
              AND l.endtime > b.range_start
            UNION ALL
            SELECT s.equipmentno, s.equipmentstate::int AS equipmentstate,
                   s.starttime, b.now_local AS endtime, FALSE AS ischamber
            FROM "YMES".tblemsequipmentstate s
            CROSS JOIN bounds b
            WHERE s.equipmentno = ANY(@mainEquipmentNos)
              AND s.starttime < b.range_end
              AND b.now_local <= b.range_end
            UNION ALL
            SELECT s.equipmentno, s.equipmentstate::int AS equipmentstate,
                   s.starttime, b.now_local AS endtime, TRUE AS ischamber
            FROM "YMES".tblemsequipmentstate s
            CROSS JOIN bounds b
            WHERE s.equipmentno = ANY(@chamberEquipmentNos)
              AND s.starttime < b.range_end
              AND b.now_local > b.range_start
        ), selected AS (
            SELECT e.equipmentno, e.equipmentstate,
                   GREATEST(e.starttime, b.range_start) AS clipped_start,
                   LEAST(e.endtime, b.range_end, b.now_local) AS clipped_end
            FROM event_source e
            CROSS JOIN bounds b
            WHERE e.starttime < b.range_end
              AND e.endtime > b.range_start
              AND ((NOT e.ischamber AND e.endtime <= b.range_end)
                   OR (e.ischamber AND e.starttime <= b.range_end))
              AND e.starttime < b.now_local
              AND LEAST(e.endtime, b.range_end, b.now_local)
                    > GREATEST(e.starttime, b.range_start)
        )
        SELECT equipmentno, equipmentstate,
               SUM(EXTRACT(EPOCH FROM (clipped_end - clipped_start)))::numeric AS seconds
        FROM selected
        GROUP BY equipmentno, equipmentstate
        ORDER BY equipmentno COLLATE "C", equipmentstate;
        """;

    private const string MovesSql = """
        WITH equipment_groups AS (
            SELECT DISTINCT equipmentno, loggroupserial
            FROM "YMES".tblwipcont_equipment
            WHERE equipmentno = ANY(@equipmentNos) AND loggroupserial IS NOT NULL
        )
        SELECT equipment.equipmentno, COALESCE(SUM(log.goodqty), 0)::numeric AS moves
        FROM equipment_groups equipment
        JOIN "YMES".tblwiplotlog_report log
          ON log.loggroupserial = equipment.loggroupserial
         AND log.endtime > @rangeStart
         AND log.endtime <= @rangeEnd
        GROUP BY equipment.equipmentno
        ORDER BY equipment.equipmentno COLLATE "C";
        """;

    public async Task<EquipmentDailyUtilizationResponse> GetAsync(
        DateTime? requestedStartTime,
        DateTime? requestedEndTime,
        string? rawAreaNos,
        string? rawEquipmentNos,
        bool chamberManage,
        CancellationToken cancellationToken)
    {
        if (requestedStartTime is null || requestedEndTime is null)
            throw Rule(400, "StartTime 與 EndTime 為必填本地時間。");

        var startTime = requestedStartTime.Value;
        var endTime = requestedEndTime.Value;
        if (startTime.Kind != DateTimeKind.Unspecified || endTime.Kind != DateTimeKind.Unspecified)
            throw Rule(400, "時間請使用台灣本地時間，格式為 YYYY-MM-DDTHH:mm:ss，不要附加時區。");
        if (startTime >= endTime)
            throw Rule(400, "StartTime 必須早於 EndTime。");

        var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TaipeiTimeZone).DateTime;
        if (endTime > nowLocal)
            throw Rule(400, "EndTime 不可晚於目前台灣時間。");
        if (endTime - startTime > TimeSpan.FromDays(MaxRangeDays))
            throw Rule(400, $"查詢區間不可超過 {MaxRangeDays} 天。");

        var areaNos = ParseList(rawAreaNos, "AreaNo");
        var equipmentNos = ParseList(rawEquipmentNos, "EquipmentNo");
        var rangeStart = DateTime.SpecifyKind(startTime, DateTimeKind.Unspecified);
        var rangeEnd = DateTime.SpecifyKind(endTime, DateTimeKind.Unspecified);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction))
            await readOnly.ExecuteNonQueryAsync(cancellationToken);

        var states = await ReadStatesAsync(connection, transaction, cancellationToken);
        var candidates = await ReadCandidatesAsync(connection, transaction, areaNos, equipmentNos,
            chamberManage, cancellationToken);
        if (candidates.Count > MaxEquipmentRows)
            throw Rule(413, $"符合條件的設備超過 {MaxEquipmentRows} 台，請縮小區域或設備條件。");

        if (candidates.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return new(startTime, endTime, chamberManage, states, []);
        }

        var mainNos = candidates.Where(x => !x.IsChamber).Select(x => x.EquipmentNo).ToArray();
        var chamberNos = candidates.Where(x => x.IsChamber).Select(x => x.EquipmentNo).ToArray();
        var allNos = candidates.Select(x => x.EquipmentNo).ToArray();
        var stateSeconds = await ReadStateSecondsAsync(connection, transaction, mainNos, chamberNos,
            rangeStart, rangeEnd, cancellationToken);
        var moves = await ReadMovesAsync(connection, transaction, allNos, rangeStart, rangeEnd, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var rowsByEquipment = new Dictionary<string, EquipmentDailyUtilizationRow>(StringComparer.Ordinal);
        foreach (var candidate in candidates.Where(x => x.IsChamber))
        {
            var row = BuildRow(candidate, states, stateSeconds, moves,
                (rangeEnd - rangeStart).TotalSeconds, []);
            rowsByEquipment[candidate.EquipmentNo] = row;
        }

        foreach (var parent in candidates.Where(x => !x.IsChamber))
        {
            var children = chamberManage
                ? candidates.Where(x => x.IsChamber &&
                    string.Equals(x.RootEquipmentNo, parent.EquipmentNo, StringComparison.Ordinal))
                    .Select(child => rowsByEquipment[child.EquipmentNo]).ToArray()
                : [];
            rowsByEquipment[parent.EquipmentNo] = BuildRow(parent, states, stateSeconds, moves,
                (rangeEnd - rangeStart).TotalSeconds, children);
        }

        var rows = candidates.Where(x => !x.IsChamber)
            .Select(x => rowsByEquipment[x.EquipmentNo]).ToArray();
        return new(startTime, endTime, chamberManage, states, rows);
    }

    private static async Task<IReadOnlyList<EquipmentDailyUtilizationState>> ReadStatesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var result = new List<EquipmentDailyUtilizationState>();
        await using var command = new NpgsqlCommand(StateSql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var color = reader.GetInt64(4);
            var colorHex = reader.GetString(5);
            if (colorHex.Length == 0)
                colorHex = $"#{unchecked((uint)color) & 0x00FFFFFF:X6}";
            else if (!colorHex.StartsWith('#'))
                colorHex = $"#{colorHex}";

            result.Add(new(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2),
                reader.GetInt32(3), color, colorHex));
        }
        return result;
    }

    private async Task<List<EquipmentCandidate>> ReadCandidatesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string[] areaNos,
        string[] equipmentNos,
        bool chamberManage,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(CandidateSql, connection, transaction);
        AddTextArray(command, "areaNos", areaNos);
        AddTextArray(command, "equipmentNos", equipmentNos);
        command.Parameters.Add("chamberManage", NpgsqlDbType.Boolean).Value = chamberManage;
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxEquipmentRows + 1;

        var result = new List<EquipmentCandidate>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new EquipmentCandidate(reader.GetString(0), reader.GetString(1),
                NullableString(reader, 2), NullableString(reader, 3), NullableString(reader, 4),
                reader.GetBoolean(5), reader.GetString(6)));
        }
        return result;
    }

    private static async Task<Dictionary<(string EquipmentNo, int State), decimal>> ReadStateSecondsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string[] mainEquipmentNos,
        string[] chamberEquipmentNos,
        DateTime rangeStart,
        DateTime rangeEnd,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<(string EquipmentNo, int State), decimal>();
        await using var command = new NpgsqlCommand(StateSecondsSql, connection, transaction);
        AddTextArray(command, "mainEquipmentNos", mainEquipmentNos);
        AddTextArray(command, "chamberEquipmentNos", chamberEquipmentNos);
        command.Parameters.Add("rangeStart", NpgsqlDbType.Timestamp).Value = rangeStart;
        command.Parameters.Add("rangeEnd", NpgsqlDbType.Timestamp).Value = rangeEnd;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add((reader.GetString(0), reader.GetInt32(1)), reader.GetDecimal(2));
        return result;
    }

    private static async Task<Dictionary<string, decimal>> ReadMovesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string[] equipmentNos,
        DateTime rangeStart,
        DateTime rangeEnd,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, decimal>(StringComparer.Ordinal);
        await using var command = new NpgsqlCommand(MovesSql, connection, transaction);
        AddTextArray(command, "equipmentNos", equipmentNos);
        command.Parameters.Add("rangeStart", NpgsqlDbType.Timestamp).Value = rangeStart;
        command.Parameters.Add("rangeEnd", NpgsqlDbType.Timestamp).Value = rangeEnd;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(reader.GetString(0), reader.GetDecimal(1));
        return result;
    }

    private static EquipmentDailyUtilizationRow BuildRow(
        EquipmentCandidate candidate,
        IReadOnlyList<EquipmentDailyUtilizationState> states,
        IReadOnlyDictionary<(string EquipmentNo, int State), decimal> stateSeconds,
        IReadOnlyDictionary<string, decimal> moves,
        double totalSeconds,
        IReadOnlyList<EquipmentDailyUtilizationRow> children)
    {
        var percentages = states.Select(state =>
        {
            stateSeconds.TryGetValue((candidate.EquipmentNo, state.EquipmentState), out var seconds);
            var percent = decimal.Round(seconds / (decimal)totalSeconds * 100m, 2,
                MidpointRounding.AwayFromZero);
            return new EquipmentDailyUtilizationStatePercent(state.EquipmentState, percent, seconds);
        }).ToArray();
        var hasStateData = percentages.Any(x => x.Seconds > 0);
        var totalPercent = percentages.Sum(x => x.Percent);
        var lossPercent = hasStateData ? Math.Max(0m, decimal.Round(100m - totalPercent, 2)) : 0m;
        moves.TryGetValue(candidate.EquipmentNo, out var moveQty);

        return new(candidate.EquipmentNo, candidate.EquipmentType, candidate.EquipmentName,
            candidate.AreaNo, candidate.BelongEquipmentNo, candidate.IsChamber,
            moveQty, moveQty, hasStateData, lossPercent, percentages, children);
    }

    private static string[] ParseList(string? raw, string label)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        var values = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (values.Any(value => value.Length > 50))
            throw Rule(400, $"{label}單一條件不可超過 50 個字元。");
        if (values.Length > MaxListValues)
            throw Rule(400, $"{label}最多可指定 {MaxListValues} 個值。");
        return values;
    }

    private static void AddTextArray(NpgsqlCommand command, string name, string[] values) =>
        command.Parameters.Add(name, NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = values;

    private static string? NullableString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static TimeZoneInfo FindTaipeiTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time"); }
    }

    private static EquipmentDailyUtilizationRuleException Rule(int statusCode, string message) =>
        new(statusCode, message);

    private sealed record EquipmentCandidate(
        string EquipmentNo,
        string EquipmentType,
        string? EquipmentName,
        string? AreaNo,
        string? BelongEquipmentNo,
        bool IsChamber,
        string RootEquipmentNo);
}
