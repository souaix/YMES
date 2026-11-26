using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Reports;

public sealed class OpStateBoardRepository(NpgsqlDataSource dataSource)
{
    private const int MaxOpNos = 100;
    private const int MaxBreakdownRows = 2_000;
    private const int MaxPage = 10_000;
    private const int MaxPageSize = 100;
    private static readonly int[] SupportedStatuses = [0, 1, 2, 3];

    private const string ValidateOpsSql = """
        SELECT opno
        FROM "YMES".tblopbasis
        WHERE opno = ANY(@opNos);
        """;

    private const string BoardSql = """
        SELECT o.opno, o.opname, o.oporder, o.psno,
               COUNT(s.lotno) FILTER (WHERE s.status = 0)::bigint,
               COALESCE(SUM(s.curqty) FILTER (WHERE s.status = 0), 0)::numeric,
               COUNT(s.lotno) FILTER (WHERE s.status = 1)::bigint,
               COALESCE(SUM(s.curqty) FILTER (WHERE s.status = 1), 0)::numeric,
               COUNT(s.lotno) FILTER (WHERE s.status = 3)::bigint,
               COALESCE(SUM(s.curqty) FILTER (WHERE s.status = 3), 0)::numeric,
               COUNT(s.lotno) FILTER (WHERE s.status = 2)::bigint,
               COALESCE(SUM(s.curqty) FILTER (WHERE s.status = 2), 0)::numeric
        FROM "YMES".tblopbasis o
        LEFT JOIN "YMES".tblwiplotstate s
          ON s.opno = o.opno AND s.status = ANY(@statuses)
        WHERE o.opno = ANY(@opNos)
        GROUP BY o.opno, o.opname, o.oporder, o.psno
        ORDER BY o.psno COLLATE "C" NULLS LAST,
                 o.oporder NULLS LAST,
                 o.opno COLLATE "C";
        """;

    private const string ProductTypeBreakdownSql = """
        SELECT s.opno, p.producttype,
               COUNT(*) FILTER (WHERE s.status = 0)::bigint,
               COALESCE(SUM(s.curqty) FILTER (WHERE s.status = 0), 0)::numeric,
               COUNT(*) FILTER (WHERE s.status = 1)::bigint,
               COALESCE(SUM(s.curqty) FILTER (WHERE s.status = 1), 0)::numeric,
               COUNT(*) FILTER (WHERE s.status = 3)::bigint,
               COALESCE(SUM(s.curqty) FILTER (WHERE s.status = 3), 0)::numeric,
               COUNT(*) FILTER (WHERE s.status = 2)::bigint,
               COALESCE(SUM(s.curqty) FILTER (WHERE s.status = 2), 0)::numeric
        FROM "YMES".tblwiplotstate s
        JOIN "YMES".tblwiplotbasis b ON b.baselotno = s.baselotno
        JOIN "YMES".tblprdproductbasis p
          ON p.productno = b.productno AND p.productversion = b.productversion
        WHERE s.opno = ANY(@opNos)
          AND s.status = ANY(@statuses)
        GROUP BY s.opno, p.producttype
        ORDER BY s.opno COLLATE "C", p.producttype COLLATE "C"
        LIMIT @take;
        """;

    private const string ProductNoBreakdownSql = """
        SELECT s.opno, b.productno,
               COUNT(*) FILTER (WHERE s.status = 0)::bigint,
               COALESCE(SUM(s.curqty) FILTER (WHERE s.status = 0), 0)::numeric,
               COUNT(*) FILTER (WHERE s.status = 1)::bigint,
               COALESCE(SUM(s.curqty) FILTER (WHERE s.status = 1), 0)::numeric,
               COUNT(*) FILTER (WHERE s.status = 3)::bigint,
               COALESCE(SUM(s.curqty) FILTER (WHERE s.status = 3), 0)::numeric,
               COUNT(*) FILTER (WHERE s.status = 2)::bigint,
               COALESCE(SUM(s.curqty) FILTER (WHERE s.status = 2), 0)::numeric
        FROM "YMES".tblwiplotstate s
        JOIN "YMES".tblwiplotbasis b ON b.baselotno = s.baselotno
        JOIN "YMES".tblprdproductbasis p
          ON p.productno = b.productno AND p.productversion = b.productversion
        WHERE s.opno = ANY(@opNos)
          AND s.status = ANY(@statuses)
          AND (@productType IS NULL OR upper(p.producttype) = @productType)
        GROUP BY s.opno, b.productno
        ORDER BY s.opno COLLATE "C", b.productno COLLATE "C"
        LIMIT @take;
        """;

    private const string LotsSql = """
        SELECT s.opno, s.lotno, b.productno, p.productname, b.productversion,
               p.producttype, s.brno, s.status::int, s.curqty, s.curunitno
        FROM "YMES".tblwiplotstate s
        JOIN "YMES".tblwiplotbasis b ON b.baselotno = s.baselotno
        JOIN "YMES".tblprdproductbasis p
          ON p.productno = b.productno AND p.productversion = b.productversion
        WHERE s.opno = ANY(@opNos)
          AND s.status = ANY(@statuses)
          AND (@productType IS NULL OR upper(p.producttype) = @productType)
          AND (@productNo IS NULL OR upper(b.productno) = @productNo)
        ORDER BY s.opno COLLATE "C", s.status, b.productno COLLATE "C", s.lotno COLLATE "C"
        LIMIT @take OFFSET @skip;
        """;

    public async Task<OpStateBoardResponse> GetBoardAsync(
        string? rawOpNos,
        CancellationToken cancellationToken)
    {
        var opNos = ParseOpNos(rawOpNos);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await BeginReadOnlyAsync(connection, cancellationToken);
        await ValidateOpNosAsync(connection, transaction, opNos, cancellationToken);

        var items = new List<OpStateBoardItem>(opNos.Length);
        await using (var command = new NpgsqlCommand(BoardSql, connection, transaction))
        {
            AddTextArray(command, "opNos", opNos);
            AddIntArray(command, "statuses", SupportedStatuses);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new OpStateBoardItem(
                    reader.GetString(0), reader.GetString(1), NullableDecimal(reader, 2),
                    NullableString(reader, 3),
                    ReadMetric(reader, 4), ReadMetric(reader, 6),
                    ReadMetric(reader, 8), ReadMetric(reader, 10)));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return new OpStateBoardResponse(items);
    }

    public async Task<OpStateBreakdownResponse> GetBreakdownAsync(
        string? rawOpNos,
        string? rawGroupBy,
        string? rawProductType,
        CancellationToken cancellationToken)
    {
        var opNos = ParseOpNos(rawOpNos);
        var groupBy = ParseGroupBy(rawGroupBy);
        var productType = Filter(rawProductType, "ProductType", 50, uppercase: true);
        if (groupBy == "productType" && productType is not null)
            throw Rule(400, "groupBy=productType 時不可再指定 ProductType。");

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await BeginReadOnlyAsync(connection, cancellationToken);
        await ValidateOpNosAsync(connection, transaction, opNos, cancellationToken);

        var sql = groupBy == "productType" ? ProductTypeBreakdownSql : ProductNoBreakdownSql;
        var items = new List<OpStateBreakdownItem>();
        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            AddTextArray(command, "opNos", opNos);
            AddIntArray(command, "statuses", SupportedStatuses);
            AddText(command, "productType", productType, 50);
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = MaxBreakdownRows + 1;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (items.Count == MaxBreakdownRows)
                    throw Rule(413, $"產品分類結果超過 {MaxBreakdownRows} 列，請縮小作業站範圍。");

                items.Add(new OpStateBreakdownItem(
                    reader.GetString(0), reader.GetString(1),
                    ReadMetric(reader, 2), ReadMetric(reader, 4),
                    ReadMetric(reader, 6), ReadMetric(reader, 8)));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return new OpStateBreakdownResponse(groupBy, items);
    }

    public async Task<OpStateLotsPage> GetLotsAsync(
        string? rawOpNos,
        string? rawStatuses,
        string? rawProductType,
        string? rawProductNo,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page is < 1 or > MaxPage)
            throw Rule(400, $"頁碼需介於 1 到 {MaxPage}。");
        if (pageSize is < 1 or > MaxPageSize)
            throw Rule(400, $"每頁筆數需介於 1 到 {MaxPageSize}。");

        var opNos = ParseOpNos(rawOpNos);
        var statuses = ParseStatuses(rawStatuses);
        var productType = Filter(rawProductType, "ProductType", 50, uppercase: true);
        var productNo = Filter(rawProductNo, "ProductNo", 50, uppercase: true);
        var offset = checked((page - 1) * pageSize);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await BeginReadOnlyAsync(connection, cancellationToken);
        await ValidateOpNosAsync(connection, transaction, opNos, cancellationToken);

        var items = new List<OpStateLotRow>(pageSize + 1);
        await using (var command = new NpgsqlCommand(LotsSql, connection, transaction))
        {
            AddTextArray(command, "opNos", opNos);
            AddIntArray(command, "statuses", statuses);
            AddText(command, "productType", productType, 50);
            AddText(command, "productNo", productNo, 50);
            command.Parameters.Add("take", NpgsqlDbType.Integer).Value = pageSize + 1;
            command.Parameters.Add("skip", NpgsqlDbType.Integer).Value = offset;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new OpStateLotRow(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2),
                    reader.GetString(3), reader.GetString(4), reader.GetString(5),
                    reader.GetString(6), reader.GetInt32(7), reader.GetDecimal(8),
                    reader.GetString(9)));
            }
        }

        var hasMore = items.Count > pageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);
        await transaction.CommitAsync(cancellationToken);
        return new OpStateLotsPage(page, pageSize, hasMore, items);
    }

    private static async Task<NpgsqlTransaction> BeginReadOnlyAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        await using var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction);
        await readOnly.ExecuteNonQueryAsync(cancellationToken);
        return transaction;
    }

    private static async Task ValidateOpNosAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string[] opNos,
        CancellationToken cancellationToken)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = new NpgsqlCommand(ValidateOpsSql, connection, transaction);
        AddTextArray(command, "opNos", opNos);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            found.Add(reader.GetString(0));

        var missing = opNos.Where(value => !found.Contains(value)).ToArray();
        if (missing.Length > 0)
            throw Rule(400, $"找不到作業站：{string.Join(", ", missing)}。");
    }

    private static string[] ParseOpNos(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw Rule(400, "請至少指定一個作業站 OPNo。");
        if (raw.Length > 4_096)
            throw Rule(400, "OPNo 條件過長。");

        var values = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (values.Length == 0)
            throw Rule(400, "請至少指定一個作業站 OPNo。");
        if (values.Length > MaxOpNos)
            throw Rule(400, $"OPNo 最多可指定 {MaxOpNos} 個值。");
        if (values.Any(value => value.Length > 20))
            throw Rule(400, "OPNo 單一值不可超過 20 個字元。");
        return values;
    }

    private static int[] ParseStatuses(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return SupportedStatuses;

        var values = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (values.Length == 0 || values.Any(value =>
                !int.TryParse(value, out var status) || !SupportedStatuses.Contains(status)))
            throw Rule(400, "Status 僅接受 0、1、2、3。");
        return values.Select(int.Parse).Order().ToArray();
    }

    private static string ParseGroupBy(string? raw)
    {
        var value = raw?.Trim().ToLowerInvariant();
        return value switch
        {
            "producttype" => "productType",
            "productno" => "productNo",
            _ => throw Rule(400, "groupBy 僅接受 productType 或 productNo。")
        };
    }

    private static string? Filter(string? raw, string label, int maxLength, bool uppercase = false)
    {
        var value = raw?.Trim();
        if (string.IsNullOrEmpty(value)) return null;
        if (value.Length > maxLength)
            throw Rule(400, $"{label} 不可超過 {maxLength} 個字元。");
        return uppercase ? value.ToUpperInvariant() : value;
    }

    private static OpStateMetric ReadMetric(NpgsqlDataReader reader, int ordinal) =>
        new(reader.GetInt64(ordinal), reader.GetDecimal(ordinal + 1));

    private static decimal? NullableDecimal(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);

    private static string? NullableString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static void AddText(NpgsqlCommand command, string name, string? value, int length) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, length).Value = (object?)value ?? DBNull.Value;

    private static void AddTextArray(NpgsqlCommand command, string name, string[] values) =>
        command.Parameters.Add(name, NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = values;

    private static void AddIntArray(NpgsqlCommand command, string name, int[] values) =>
        command.Parameters.Add(name, NpgsqlDbType.Array | NpgsqlDbType.Integer).Value = values;

    private static OpStateRuleException Rule(int statusCode, string message) =>
        new(statusCode, message);
}
