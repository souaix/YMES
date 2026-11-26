using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Reports;

public sealed class ConfirmLotRepository(NpgsqlDataSource dataSource)
{
    private const int MaxPage = 10_000;
    private const int MaxPageSize = 100;
    private const int MaxListValues = 100;

    private const string Sql = """
        SELECT b.productno, e.productname, b.productversion,
               a.lotno, a.opno, a.brno, a.status::int, a.curqty, a.curunitno
        FROM "YMES".tblwiplotstate a
        JOIN "YMES".tblwiplotbasis b ON b.baselotno = a.baselotno
        LEFT JOIN "YMES".tblopbasis c ON c.opno = a.opno
        LEFT JOIN "YMES".tblprsprocessbasis d
          ON d.processno = a.processno AND d.processversion = a.processversion
        JOIN "YMES".tblprdproductbasis e
          ON e.productno = b.productno AND e.productversion = b.productversion
        LEFT JOIN "YMES".tblentcustomerbasis f ON f.customerno = b.customerno
        WHERE b.lotstate = 1
          AND (
              a.status BETWEEN 0 AND 5
              OR (a.status = 11 AND EXISTS (
                  SELECT 1 FROM "YMES".tblinvwipinventory_semi semi
                  WHERE semi.baselotno = a.baselotno
                  GROUP BY semi.baselotno
                  HAVING SUM(semi.qty) > 0
              ))
              OR (a.status = 12 AND (
                  EXISTS (
                      SELECT 1 FROM "YMES".tblinvwip_scrap scrap
                      WHERE scrap.baselotno = a.baselotno
                      GROUP BY scrap.baselotno
                      HAVING COALESCE(SUM(scrap.scrapqty), 0) > 0
                          OR COALESCE(SUM(scrap.lossqty), 0) > 0
                  )
                  OR EXISTS (
                      SELECT 1 FROM "YMES".tblinvwip_failbin failbin
                      WHERE failbin.baselotno = a.baselotno
                  )
                  OR EXISTS (
                      SELECT 1 FROM "YMES".tblinvwip_componentscrap componentscrap
                      WHERE componentscrap.baselotno = a.baselotno
                  )
              ))
          )
          AND (@roNo IS NULL OR b.rono ILIKE @roNo)
          AND (@moNo IS NULL OR b.mono ILIKE @moNo)
          AND (@productNo IS NULL OR b.productno ILIKE @productNo)
          AND (cardinality(@productVersions) = 0 OR b.productversion = ANY(@productVersions))
          AND (@customerNo IS NULL OR f.customerno = @customerNo)
          AND (cardinality(@lotNos) = 0 OR a.lotno = ANY(@lotNos))
          AND (@opNo IS NULL OR c.opno = @opNo)
          AND (@orgLotNo IS NULL OR b.orglotno ILIKE @orgLotNo)
          AND (@psNo IS NULL OR d.psno = @psNo)
        ORDER BY a.lotno COLLATE "C", a.opno COLLATE "C", a.status
        LIMIT @take OFFSET @skip;
        """;

    public async Task<ConfirmLotPage> ListAsync(
        string? rawRoNo,
        string? rawMoNo,
        string? rawProductNo,
        string? rawProductVersions,
        string? rawCustomerNo,
        string? rawLotNos,
        string? rawOpNo,
        string? rawOrgLotNo,
        string? rawPsNo,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page is < 1 or > MaxPage)
            throw Rule(400, $"頁碼需介於 1 到 {MaxPage}。");
        if (pageSize is < 1 or > MaxPageSize)
            throw Rule(400, $"每頁筆數需介於 1 到 {MaxPageSize}。");

        var roNo = Filter(rawRoNo, "RO No", 25, uppercase: true);
        var moNo = Filter(rawMoNo, "MO No", 50, uppercase: true);
        var productNo = Filter(rawProductNo, "Product No", 50, uppercase: true);
        var customerNo = Filter(rawCustomerNo, "Customer No", 50, uppercase: true);
        var opNo = Filter(rawOpNo, "OP No", 20, uppercase: true);
        var orgLotNo = Filter(rawOrgLotNo, "Org Lot No", 50, uppercase: true);
        var psNo = Filter(rawPsNo, "PS No", 50, uppercase: true);
        var productVersions = ParseList(rawProductVersions, "Product Version", 5);
        var lotNos = ParseList(rawLotNos, "Lot No", 50).Select(value => value.ToUpperInvariant()).ToArray();

        if (roNo is null && moNo is null && productNo is null && productVersions.Length == 0 &&
            customerNo is null && lotNos.Length == 0 && opNo is null && orgLotNo is null && psNo is null)
            throw Rule(400, "請至少輸入一項查詢條件後再查詢。");

        var offset = checked((page - 1) * pageSize);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction))
            await readOnly.ExecuteNonQueryAsync(cancellationToken);

        await using var command = new NpgsqlCommand(Sql, connection, transaction);
        AddText(command, "roNo", roNo, 25);
        AddText(command, "moNo", moNo, 50);
        AddText(command, "productNo", productNo, 50);
        AddTextArray(command, "productVersions", productVersions);
        AddText(command, "customerNo", customerNo, 50);
        AddTextArray(command, "lotNos", lotNos);
        AddText(command, "opNo", opNo, 20);
        AddText(command, "orgLotNo", orgLotNo, 50);
        AddText(command, "psNo", psNo, 50);
        command.Parameters.Add("take", NpgsqlDbType.Integer).Value = pageSize + 1;
        command.Parameters.Add("skip", NpgsqlDbType.Integer).Value = offset;

        var items = new List<ConfirmLotRow>(pageSize + 1);
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new ConfirmLotRow(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2),
                    reader.GetString(3), reader.GetString(4), reader.GetString(5),
                    reader.GetInt32(6), reader.GetDecimal(7), reader.GetString(8)));
            }
        }

        var hasMore = items.Count > pageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);
        await transaction.CommitAsync(cancellationToken);
        return new ConfirmLotPage(page, pageSize, hasMore, items);
    }

    private static string? Filter(string? raw, string label, int maxLength, bool uppercase = false)
    {
        var value = raw?.Trim();
        if (string.IsNullOrEmpty(value)) return null;
        if (value.Length > maxLength)
            throw Rule(400, $"{label}不可超過 {maxLength} 個字元。");
        return uppercase ? value.ToUpperInvariant() : value;
    }

    private static string[] ParseList(string? raw, string label, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        var values = raw.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (values.Length == 0) throw Rule(400, $"{label}至少需要一個值。");
        if (values.Length > MaxListValues)
            throw Rule(400, $"{label}最多可指定 {MaxListValues} 個值。");
        if (values.Any(value => value.Length > maxLength))
            throw Rule(400, $"{label}單一值不可超過 {maxLength} 個字元。");
        return values;
    }

    private static void AddText(NpgsqlCommand command, string name, string? value, int length) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar, length).Value = (object?)value ?? DBNull.Value;

    private static void AddTextArray(NpgsqlCommand command, string name, string[] values) =>
        command.Parameters.Add(name, NpgsqlDbType.Array | NpgsqlDbType.Varchar).Value = values;

    private static ConfirmLotRuleException Rule(int statusCode, string message) =>
        new(statusCode, message);
}
