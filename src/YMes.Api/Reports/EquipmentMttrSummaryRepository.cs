using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Reports;

public sealed class EquipmentMttrSummaryRepository(NpgsqlDataSource dataSource)
{
    private const int MaxRangeDays = 366;
    private const int MaxListValues = 100;
    private const int MaxEquipmentRows = 2_000;
    private const string BreakdownControlType = "EQP Break Down";
    private const string BreakdownControlNo = "BreakDown";
    private static readonly TimeZoneInfo TaipeiTimeZone = FindTaipeiTimeZone();

    private const string CandidateSql = """
        SELECT e.equipmentno, e.equipmenttype,
               NULLIF(e.belongequipmentno, '') AS belongequipmentno,
               COALESCE(e.chamberflag, 0)::int = 1 AS is_chamber,
               area.containareano, area.baseareano
        FROM "YMES".tbleqpequipmentbasis e
        LEFT JOIN LATERAL (
            SELECT r.containareano, r.baseareano
            FROM "YMES".tblsmdarearelation r
            WHERE r.objecttype = 2
              AND r.objectno = CASE WHEN COALESCE(e.chamberflag, 0) = 1
                                    THEN e.belongequipmentno ELSE e.equipmentno END
            ORDER BY r.containareano COLLATE "C", r.baseareano COLLATE "C"
            LIMIT 1
        ) area ON TRUE
        WHERE e.issuestate = 2
          AND (@includeChambers OR COALESCE(e.chamberflag, 0) = 0)
          AND (cardinality(@areaNos) = 0 OR area.containareano = ANY(@areaNos)
               OR area.baseareano = ANY(@areaNos))
          AND (cardinality(@equipmentTypes) = 0 OR upper(e.equipmenttype) = ANY(@equipmentTypes))
          AND (cardinality(@equipmentNos) = 0 OR upper(e.equipmentno) = ANY(@equipmentNos))
        ORDER BY e.equipmentno COLLATE "C"
        LIMIT @take;
        """;

    private const string SummarySql = """
        WITH bounds AS (
            SELECT @rangeStart::timestamp without time zone AS range_start,
                   @rangeEnd::timestamp without time zone AS range_end,
                   timezone('Asia/Taipei', now()) AS now_local
        ), event_source AS (
            SELECT l.equipmentno, l.equipmentstate::int AS equipmentstate,
                   l.starttime, l.endtime
            FROM "YMES".tblemsequipmentstatelog l
            WHERE l.equipmentno = ANY(@equipmentNos)
            UNION ALL
            SELECT s.equipmentno, s.equipmentstate::int AS equipmentstate,
                   s.starttime, b.now_local AS endtime
            FROM "YMES".tblemsequipmentstate s
            CROSS JOIN bounds b
            WHERE s.equipmentno = ANY(@equipmentNos)
              AND s.starttime < b.now_local
        ), clipped AS (
            SELECT e.equipmentno, e.equipmentstate,
                   GREATEST(e.starttime, b.range_start) AS clipped_start,
                   LEAST(e.endtime, b.range_end, b.now_local) AS clipped_end
            FROM event_source e
            CROSS JOIN bounds b
            WHERE e.starttime < b.range_end
              AND e.endtime > b.range_start
              AND e.starttime < b.now_local
              AND LEAST(e.endtime, b.range_end, b.now_local)
                    > GREATEST(e.starttime, b.range_start)
        )
        SELECT equipmentno,
               COUNT(*) FILTER (WHERE equipmentstate = @breakdownState)::bigint AS breakdown_times,
               COALESCE(SUM(EXTRACT(EPOCH FROM (clipped_end - clipped_start)) / 3600.0)
                   FILTER (WHERE equipmentstate = @breakdownState), 0)::numeric AS breakdown_hours,
               COALESCE(SUM(EXTRACT(EPOCH FROM (clipped_end - clipped_start)) / 3600.0)
                   FILTER (WHERE equipmentstate <> @breakdownState), 0)::numeric AS non_breakdown_hours,
               COALESCE(SUM(EXTRACT(EPOCH FROM (clipped_end - clipped_start)) / 3600.0), 0)::numeric AS total_hours
        FROM clipped
        GROUP BY equipmentno
        ORDER BY equipmentno COLLATE "C";
        """;

    public async Task<EquipmentMttrSummaryResponse> GetAsync(
        DateOnly? requestedStartDate,
        DateOnly? requestedEndDate,
        string? rawAreaNos,
        string? rawEquipmentTypes,
        string? rawEquipmentNos,
        bool includeChambers,
        CancellationToken cancellationToken)
    {
        if (requestedStartDate is null || requestedEndDate is null)
            throw Rule(400, "StartDate 與 EndDate 為必填日期。");

        var startDate = requestedStartDate.Value;
        var endDate = requestedEndDate.Value;
        if (startDate > endDate)
            throw Rule(400, "StartDate 不可晚於 EndDate。");
        if (endDate > DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TaipeiTimeZone).DateTime))
            throw Rule(400, "EndDate 不可晚於今日。");
        if (endDate.DayNumber - startDate.DayNumber + 1 > MaxRangeDays)
            throw Rule(400, $"查詢區間不可超過 {MaxRangeDays} 天。");

        var areaNos = ParseList(rawAreaNos, "AreaNo");
        var equipmentTypes = ParseList(rawEquipmentTypes, "EquipmentType");
        var equipmentNos = ParseList(rawEquipmentNos, "EquipmentNo");
        var rangeStart = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var rangeEnd = endDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction))
            await readOnly.ExecuteNonQueryAsync(cancellationToken);

        var breakdownState = await ReadBreakdownStateAsync(connection, transaction, cancellationToken);
        var candidates = await ReadCandidatesAsync(connection, transaction, areaNos, equipmentTypes,
            equipmentNos, includeChambers, cancellationToken);
        if (candidates.Count > MaxEquipmentRows)
            throw Rule(413, $"符合條件的設備超過 {MaxEquipmentRows} 台，請縮小區域、類型或設備條件。");

        if (candidates.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return new(startDate, endDate, includeChambers, []);
        }

        var summaries = await ReadSummariesAsync(connection, transaction, candidates.Select(x => x.EquipmentNo).ToArray(),
            rangeStart, rangeEnd, breakdownState, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var rows = new List<EquipmentMttrSummaryRow>(summaries.Count);
        foreach (var candidate in candidates)
        {
            if (!summaries.TryGetValue(candidate.EquipmentNo, out var summary))
                continue;

            var mttr = summary.BreakdownTimes > 0
                ? summary.BreakdownHours / summary.BreakdownTimes
                : 0m;
            var mtbf = summary.BreakdownTimes > 0
                ? summary.NonBreakdownHours / summary.BreakdownTimes
                : summary.NonBreakdownHours;
            var availability = summary.TotalHours > 0
                ? summary.NonBreakdownHours / summary.TotalHours
                : 0m;

            rows.Add(new(candidate.EquipmentNo, candidate.EquipmentType, candidate.AreaNo,
                candidate.BelongEquipmentNo, candidate.IsChamber, summary.BreakdownTimes,
                mttr, mtbf, availability));
        }

        return new(startDate, endDate, includeChambers, rows);
    }

    private static async Task<int> ReadBreakdownStateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT min(equipmentstate)::int, count(DISTINCT equipmentstate)::int
            FROM "YMES".tbleqpstatecontrol
            WHERE controltype = @controlType AND controlno = @controlNo;
            """, connection, transaction);
        AddText(command, "controlType", BreakdownControlType, 50);
        AddText(command, "controlNo", BreakdownControlNo, 50);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken) || reader.IsDBNull(0) || reader.GetInt32(1) != 1)
            throw Rule(503, "尚未設定唯一的設備故障狀態（EQP Break Down / BreakDown）。");
        return reader.GetInt32(0);
    }

    private static async Task<List<EquipmentCandidate>> ReadCandidatesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string[] areaNos,
        string[] equipmentTypes,
        string[] equipmentNos,
        bool includeChambers,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(CandidateSql, connection, transaction);
        AddTextArray(command, "areaNos", areaNos);
        AddTextArray(command, "equipmentTypes", equipmentTypes);
        AddTextArray(command, "equipmentNos", equipmentNos);
        command.Parameters.Add("includeChambers", NpgsqlDbType.Boolean).Value = includeChambers;
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxEquipmentRows + 1;

        var result = new List<EquipmentCandidate>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new EquipmentCandidate(
                reader.GetString(0), reader.GetString(1), NullableString(reader, 2), reader.GetBoolean(3),
                NullableString(reader, 4)));
        }
        return result;
    }

    private static async Task<Dictionary<string, EquipmentSummary>> ReadSummariesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string[] equipmentNos,
        DateTime rangeStart,
        DateTime rangeEnd,
        int breakdownState,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(SummarySql, connection, transaction);
        AddTextArray(command, "equipmentNos", equipmentNos);
        command.Parameters.Add("rangeStart", NpgsqlDbType.Timestamp).Value = rangeStart;
        command.Parameters.Add("rangeEnd", NpgsqlDbType.Timestamp).Value = rangeEnd;
        command.Parameters.Add("breakdownState", NpgsqlDbType.Integer).Value = breakdownState;

        var result = new Dictionary<string, EquipmentSummary>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.GetString(0), new EquipmentSummary(
                reader.GetInt64(1), reader.GetDecimal(2), reader.GetDecimal(3), reader.GetDecimal(4)));
        }
        return result;
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
            throw Rule(400, $"{label}最多可指定 {MaxListValues} 個條件。");
        return values;
    }

    private static void AddText(NpgsqlCommand command, string name, string value, int length) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, length).Value = value;

    private static void AddTextArray(NpgsqlCommand command, string name, string[] values) =>
        command.Parameters.Add(name, NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = values;

    private static string? NullableString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static TimeZoneInfo FindTaipeiTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time"); }
    }

    private static EquipmentMttrSummaryRuleException Rule(int statusCode, string message) =>
        new(statusCode, message);

    private sealed record EquipmentCandidate(
        string EquipmentNo,
        string EquipmentType,
        string? BelongEquipmentNo,
        bool IsChamber,
        string? AreaNo);

    private sealed record EquipmentSummary(
        long BreakdownTimes,
        decimal BreakdownHours,
        decimal NonBreakdownHours,
        decimal TotalHours);
}
