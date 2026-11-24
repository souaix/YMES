using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/wip/erfs")]
public sealed class WipErfBasisController(NpgsqlDataSource dataSource) : ControllerBase
{
    private readonly WipErfBasisRepository _repository = new(dataSource);

    [HttpGet("query")]
    public IActionResult Query([FromQuery] WipErfBasisQuery query)
    {
        try
        {
            return Ok(_repository.Query(query));
        }
        catch (WipErfBasisQueryException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "WP508 異常單查詢失敗", detail: ex.Message);
        }
        catch (NpgsqlException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP508 異常單查詢失敗",
                detail: "WP508 查詢結構或唯讀權限尚未就緒，請確認 YMES ERF／Lot 查詢主檔與 SELECT 權限。");
        }
    }
}

public sealed class WipErfBasisRepository(NpgsqlDataSource dataSource)
{
    private const int DefaultPageSize = 100;
    private const int MaxPageSize = 100;
    private const int MaxPage = 100_000;

    private const string BaseFrom = """
        FROM "YMES".tblwiperfbasis e
        LEFT JOIN "YMES".tblwiplotbasis lb
          ON lb.baselotno=COALESCE(NULLIF(NULLIF(e.baselotno,''),'N/A'),e.lotno)
        """;

    private const string ResultFrom = """
        FROM "YMES".tblwiperfbasis e
        LEFT JOIN "YMES".tblwiplotbasis lb
          ON lb.baselotno=COALESCE(NULLIF(NULLIF(e.baselotno,''),'N/A'),e.lotno)
        LEFT JOIN LATERAL (
            SELECT p.productname
            FROM "YMES".tblprdproductbasis p
            WHERE p.productno=COALESCE(NULLIF(e.productno,''),lb.productno)
              AND (lb.productversion IS NULL OR p.productversion=lb.productversion)
            ORDER BY p.curversion DESC,p.productversion DESC
            LIMIT 1
        ) pr ON true
        LEFT JOIN "YMES".tblopbasis op ON op.opno=e.opno
        LEFT JOIN "YMES".tblentcustomerbasis c
          ON c.customerno=COALESCE(NULLIF(e.customerno,''),lb.customerno)
        LEFT JOIN LATERAL (
            SELECT string_agg(b.lotno, ',' ORDER BY b.lotno) AS batch_lot
            FROM "YMES".tblwiperfbatchlot b
            WHERE b.erfno=e.erfno
        ) batches ON true
        """;

    private const string Filter = """
        e.erfsource=0
        AND (@lotNo IS NULL OR position(lower(@lotNo) in lower(e.lotno))>0
         OR EXISTS (SELECT 1 FROM "YMES".tblwiperfbatchlot b
                    WHERE b.erfno=e.erfno AND position(lower(@lotNo) in lower(b.lotno))>0))
        AND (@opNo IS NULL OR lower(e.opno)=lower(@opNo)
             OR EXISTS (SELECT 1 FROM "YMES".tblwiperfbatchlot b
                        WHERE b.erfno=e.erfno AND lower(b.opno)=lower(@opNo)))
        AND (@customerNo IS NULL
             OR lower(COALESCE(NULLIF(e.customerno,''),lb.customerno,''))=lower(@customerNo)
             OR EXISTS (SELECT 1
                        FROM "YMES".tblwiperfbatchlot b
                        LEFT JOIN "YMES".tblwiplotbasis member_lot ON member_lot.baselotno=b.baselotno
                        WHERE b.erfno=e.erfno AND lower(COALESCE(member_lot.customerno,''))=lower(@customerNo)))
        AND (@mClassNo IS NULL OR lower(e.mclassno)=lower(@mClassNo))
        AND (@curGroupNo IS NULL OR lower(e.curgroupno)=lower(@curGroupNo))
        AND (@erfNo IS NULL OR lower(e.erfno)=lower(@erfNo))
        AND (@createdFrom IS NULL OR e.createdate>=@createdFrom)
        AND (@createdTo IS NULL OR e.createdate<=@createdTo)
        AND (@releasedFrom IS NULL OR e.releasedate>=@releasedFrom)
        AND (@releasedTo IS NULL OR e.releasedate<=@releasedTo)
        """;

    public WipErfBasisResponse Query(WipErfBasisQuery query)
    {
        ValidatePage(query.Page, query.PageSize, out var page, out var pageSize);
        var createdRange = NormalizeRange(query.CreatedFrom, query.CreatedTo, "Create Date");
        var releasedRange = NormalizeRange(query.ReleasedFrom, query.ReleasedTo, "Release Date");
        var filters = new NormalizedFilters(
            Normalize(query.LotNo, 50, "Lot No"),
            Normalize(query.OpNo, 20, "OP No"),
            Normalize(query.CustomerNo, 50, "Customer No"),
            Normalize(query.MClassNo, 30, "MClass No"),
            Normalize(query.CurGroupNo, 100, "Cur Group"),
            Normalize(query.ErfNo, 20, "ERF No"),
            createdRange?.From, createdRange?.To, releasedRange?.From, releasedRange?.To);

        if (filters.LotNo is null && filters.OpNo is null && filters.CustomerNo is null &&
            filters.MClassNo is null && filters.CurGroupNo is null && filters.ErfNo is null &&
            filters.CreatedFrom is null && filters.ReleasedFrom is null)
            throw new WipErfBasisQueryException(400, "至少需要一個有效查詢條件。");

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);
        using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction))
            readOnly.ExecuteNonQuery();

        using var countCommand = new NpgsqlCommand($"SELECT count(*) {BaseFrom} WHERE {Filter};", connection, transaction);
        AddFilters(countCommand, filters);
        var totalCount = (long)countCommand.ExecuteScalar()!;
        var pageCount = totalCount == 0 ? 0 : checked((int)Math.Ceiling(totalCount / (double)pageSize));
        var offset = checked((page - 1) * pageSize);

        using var command = new NpgsqlCommand($"""
            SELECT e.erfno,e.lotno,
                   COALESCE(NULLIF(e.productno,''),lb.productno,''),COALESCE(pr.productname,''),
                   COALESCE(lb.customerlotno,''),e.status::int,e.opno,
                   COALESCE(NULLIF(e.customerno,''),lb.customerno,''),COALESCE(c.customername,''),
                   e.mclassno,COALESCE(e.holddescription,''),e.curgroupno,COALESCE(e.dispdescription,''),
                   COALESCE(e.ruleno,''),COALESCE(e.createqty,0)::numeric,e.createdate,
                   COALESCE(e.scrapqty,0)::numeric,COALESCE(e.curqty,0)::numeric,
                   COALESCE(e.releaseqty,0)::numeric,COALESCE(e.releaser,''),e.releasedate,
                   GREATEST(0,FLOOR(EXTRACT(EPOCH FROM
                       ((CASE WHEN e.status IN (12,21) AND e.releasedate IS NOT NULL
                              THEN e.releasedate ELSE localtimestamp END)-e.createdate))/60))::bigint,
                   COALESCE(lb.priority,99)::int,COALESCE(NULLIF(batches.batch_lot,''),NULLIF(e.batchlot,''),''),
                   COALESCE(e.qrsno,''),e.erftype::int,e.erfsource::int,e.creator
            {ResultFrom}
            WHERE {Filter}
            ORDER BY e.erfno COLLATE "C"
            LIMIT @limit OFFSET @offset;
            """, connection, transaction);
        AddFilters(command, filters);
        command.Parameters.Add("limit", NpgsqlDbType.Integer).Value = pageSize;
        command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = offset;

        var rows = new List<WipErfBasisRow>(pageSize);
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                rows.Add(new WipErfBasisRow(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetString(4), reader.GetInt32(5), reader.GetString(6), reader.GetString(7),
                    reader.GetString(8), reader.GetString(9), reader.GetString(10), reader.GetString(11),
                    reader.GetString(12), reader.GetString(13), reader.GetDecimal(14), reader.GetDateTime(15),
                    reader.GetDecimal(16), reader.GetDecimal(17), reader.GetDecimal(18), reader.GetString(19),
                    reader.IsDBNull(20) ? null : reader.GetDateTime(20), reader.GetInt64(21), reader.GetInt32(22),
                    reader.GetString(23), reader.GetString(24), reader.GetInt32(25), reader.GetInt32(26), reader.GetString(27)));
            }
        }

        transaction.Commit();
        return new WipErfBasisResponse(totalCount, page, pageSize, pageCount, rows);
    }

    private static void AddFilters(NpgsqlCommand command, NormalizedFilters filters)
    {
        AddText(command, "lotNo", filters.LotNo);
        AddText(command, "opNo", filters.OpNo);
        AddText(command, "customerNo", filters.CustomerNo);
        AddText(command, "mClassNo", filters.MClassNo);
        AddText(command, "curGroupNo", filters.CurGroupNo);
        AddText(command, "erfNo", filters.ErfNo);
        AddTimestamp(command, "createdFrom", filters.CreatedFrom);
        AddTimestamp(command, "createdTo", filters.CreatedTo);
        AddTimestamp(command, "releasedFrom", filters.ReleasedFrom);
        AddTimestamp(command, "releasedTo", filters.ReleasedTo);
    }

    private static void AddText(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static void AddTimestamp(NpgsqlCommand command, string name, DateTime? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Timestamp).Value = (object?)value ?? DBNull.Value;

    private static string? Normalize(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength || normalized.Any(char.IsControl))
            throw new WipErfBasisQueryException(400, $"{label} 條件長度或字元不正確。");
        return normalized;
    }

    private static DateTimeRange? NormalizeRange(DateTime? from, DateTime? to, string label)
    {
        if (from.HasValue != to.HasValue)
            throw new WipErfBasisQueryException(400, $"{label} 必須同時提供起始與結束時間。");
        if (!from.HasValue) return null;
        if (from.Value > to!.Value)
            throw new WipErfBasisQueryException(400, $"{label} 起始時間不可晚於結束時間。");
        if (from.Value.Kind != DateTimeKind.Unspecified || to.Value.Kind != DateTimeKind.Unspecified)
            throw new WipErfBasisQueryException(400, $"{label} 必須使用本地日期時間格式。");
        return new DateTimeRange(from.Value, to.Value);
    }

    private static void ValidatePage(int? requestedPage, int? requestedPageSize, out int page, out int pageSize)
    {
        page = requestedPage ?? 1;
        pageSize = requestedPageSize ?? DefaultPageSize;
        if (page < 1 || page > MaxPage)
            throw new WipErfBasisQueryException(400, $"頁碼必須介於 1 到 {MaxPage:N0}。");
        if (pageSize < 1 || pageSize > MaxPageSize)
            throw new WipErfBasisQueryException(400, $"每頁筆數必須介於 1 到 {MaxPageSize}。");
    }

    private sealed record NormalizedFilters(string? LotNo, string? OpNo, string? CustomerNo, string? MClassNo,
        string? CurGroupNo, string? ErfNo, DateTime? CreatedFrom, DateTime? CreatedTo,
        DateTime? ReleasedFrom, DateTime? ReleasedTo);
    private sealed record DateTimeRange(DateTime From, DateTime To);
}

public sealed class WipErfBasisQuery
{
    public string? LotNo { get; set; }
    public string? OpNo { get; set; }
    public string? CustomerNo { get; set; }
    public string? MClassNo { get; set; }
    public string? CurGroupNo { get; set; }
    public string? ErfNo { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public DateTime? ReleasedFrom { get; set; }
    public DateTime? ReleasedTo { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record WipErfBasisResponse(long TotalCount, int Page, int PageSize, int PageCount,
    IReadOnlyList<WipErfBasisRow> Items);

public sealed record WipErfBasisRow(
    string ErfNo, string LotNo, string ProductNo, string ProductName, string CustomerLotNo, int Status,
    string OpNo, string CustomerNo, string CustomerName, string MClassNo, string HoldDescription,
    string CurGroupNo, string DispDescription, string RuleNo, decimal CreateQty, DateTime CreateDate,
    decimal ErfRecordQty, decimal CurQty, decimal ReleaseQty, string Releaser, DateTime? ReleaseDate,
    long CalHoldTimeMinutes, int Priority, string BatchLot, string QrsNo, int ErfType, int ErfSource, string Creator);

public sealed class WipErfBasisQueryException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
