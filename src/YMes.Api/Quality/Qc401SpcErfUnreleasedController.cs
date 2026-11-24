using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/spc/erfs")]
public sealed class Qc401SpcErfUnreleasedController(NpgsqlDataSource dataSource) : ControllerBase
{
    private readonly Qc401SpcErfUnreleasedRepository _repository = new(dataSource);

    [HttpGet("unreleased")]
    public IActionResult GetUnreleased([FromQuery] Qc401SpcErfUnreleasedQuery query)
    {
        try
        {
            return Ok(_repository.GetUnreleased(query));
        }
        catch (Qc401QueryException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "SC401 未解除異常單查詢失敗", detail: ex.Message);
        }
        catch (PostgresException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "SC401 未解除異常單查詢失敗",
                detail: "SC401 查詢結構或唯讀權限尚未就緒，請確認 migration 0070 與 YMES 查詢主檔。 ");
        }
    }
}

public sealed class Qc401SpcErfUnreleasedRepository(NpgsqlDataSource dataSource)
{
    private const int DefaultPageSize = 100;
    private const int MaxPageSize = 100;
    private const int MaxPage = 100_000;

    private const string From = """
        FROM "YMES".tblwiperfbasis e
        LEFT JOIN "YMES".tblwiplotbasis lb ON lb.baselotno=e.lotno
        LEFT JOIN LATERAL (
            SELECT p.productname
            FROM "YMES".tblprdproductbasis p
            WHERE p.productno=COALESCE(NULLIF(e.productno,''),lb.productno)
              AND (lb.productversion IS NULL OR p.productversion=lb.productversion)
            ORDER BY p.curversion DESC,p.productversion DESC
            LIMIT 1
        ) pr ON true
        LEFT JOIN "YMES".tblopbasis op ON op.opno=e.opno
        LEFT JOIN "YMES".tblmtlmaterialbasis m ON m.materialno=e.materialno
        LEFT JOIN "YMES".tblmtlmaterialvendor v ON v.vendorno=e.vendorno
        LEFT JOIN "YMES".tblentcustomerbasis c ON c.customerno=COALESCE(NULLIF(e.customerno,''),lb.customerno)
        """;

    private const string Filter = """
        e.erfsource IN (1,2)
        AND e.status IN (0,1,2,3,11)
        AND (@lotNo IS NULL OR position(lower(@lotNo) in lower(e.lotno))>0
             OR EXISTS (SELECT 1 FROM "YMES".tblwiperfbatchlot b
                        WHERE b.erfno=e.erfno AND position(lower(@lotNo) in lower(b.lotno))>0))
        AND (@customerNo IS NULL OR lower(COALESCE(NULLIF(e.customerno,''),lb.customerno,''))=lower(@customerNo))
        AND (@opNo IS NULL OR lower(e.opno)=lower(@opNo))
        AND (@mClassNo IS NULL OR lower(e.mclassno)=lower(@mClassNo))
        AND (@curGroupNo IS NULL OR lower(e.curgroupno)=lower(@curGroupNo))
        AND (@erfNo IS NULL OR lower(e.erfno)=lower(@erfNo))
        AND (@materialNo IS NULL OR lower(e.materialno)=lower(@materialNo))
        AND (@vendorNo IS NULL OR lower(e.vendorno)=lower(@vendorNo))
        AND (@productNo IS NULL OR lower(COALESCE(NULLIF(e.productno,''),lb.productno,''))=lower(@productNo))
        """;

    public Qc401SpcErfUnreleasedResponse GetUnreleased(Qc401SpcErfUnreleasedQuery query)
    {
        ValidatePage(query.Page, query.PageSize, out var page, out var pageSize);
        var filters = new NormalizedFilters(
            Normalize(query.LotNo, 50, "Lot No"),
            Normalize(query.CustomerNo, 50, "Customer No"),
            Normalize(query.OpNo, 20, "OP No"),
            Normalize(query.MClassNo, 30, "MClass No"),
            Normalize(query.CurGroupNo, 100, "Cur Group"),
            Normalize(query.ErfNo, 20, "ERF No"),
            Normalize(query.MaterialNo, 50, "Material No"),
            Normalize(query.VendorNo, 20, "Vendor No"),
            Normalize(query.ProductNo, 50, "Product No"));

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);
        using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction))
            readOnly.ExecuteNonQuery();

        using var countCommand = new NpgsqlCommand($"SELECT count(*) {From} WHERE {Filter};", connection, transaction);
        AddFilters(countCommand, filters);
        var total = (long)countCommand.ExecuteScalar()!;
        var pageCount = total == 0 ? 0 : checked((int)Math.Ceiling(total / (double)pageSize));
        var offset = checked((page - 1) * pageSize);

        using var command = new NpgsqlCommand($"""
            SELECT e.erfno,e.lotno,COALESCE(lb.customerlotno,''),
                   COALESCE(NULLIF(e.productno,''),lb.productno,''),COALESCE(pr.productname,''),
                   e.status::int,e.opno,COALESCE(op.opname,''),
                   COALESCE(NULLIF(e.customerno,''),lb.customerno,''),COALESCE(c.customername,''),
                   e.mclassno,COALESCE(e.materialno,''),COALESCE(m.materialname,''),
                   COALESCE(e.vendorno,''),COALESCE(v.vendorname,''),COALESCE(e.holddescription,''),
                   e.curgroupno,COALESCE(e.dispdescription,''),COALESCE(e.ruleno,''),
                   COALESCE(e.createqty,0)::numeric,COALESCE(e.scrapqty,0)::numeric,
                   COALESCE(e.curqty,0)::numeric,e.createdate,COALESCE(lb.priority,99)::int,
                   e.creator,e.erftype::int,e.erfsource::int
            {From}
            WHERE {Filter}
            ORDER BY e.erfno COLLATE "C"
            LIMIT @limit OFFSET @offset;
            """, connection, transaction);
        AddFilters(command, filters);
        command.Parameters.Add("limit", NpgsqlDbType.Integer).Value = pageSize;
        command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = offset;

        var rows = new List<Qc401SpcErfUnreleasedRow>(pageSize);
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                rows.Add(new Qc401SpcErfUnreleasedRow(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
                    reader.GetInt32(5), reader.GetString(6), reader.GetString(7), reader.GetString(8), reader.GetString(9),
                    reader.GetString(10), reader.GetString(11), reader.GetString(12), reader.GetString(13), reader.GetString(14),
                    reader.GetString(15), reader.GetString(16), reader.GetString(17), reader.GetString(18),
                    reader.GetDecimal(19), reader.GetDecimal(20), reader.GetDecimal(21), reader.GetDateTime(22),
                    reader.GetInt32(23), reader.GetString(24), reader.GetInt32(25), reader.GetInt32(26)));
            }
        }

        transaction.Commit();
        return new Qc401SpcErfUnreleasedResponse(total, page, pageSize, pageCount, rows);
    }

    private static void AddFilters(NpgsqlCommand command, NormalizedFilters filters)
    {
        Add(command, "lotNo", filters.LotNo);
        Add(command, "customerNo", filters.CustomerNo);
        Add(command, "opNo", filters.OpNo);
        Add(command, "mClassNo", filters.MClassNo);
        Add(command, "curGroupNo", filters.CurGroupNo);
        Add(command, "erfNo", filters.ErfNo);
        Add(command, "materialNo", filters.MaterialNo);
        Add(command, "vendorNo", filters.VendorNo);
        Add(command, "productNo", filters.ProductNo);
    }

    private static void Add(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static string? Normalize(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength || normalized.Any(char.IsControl))
            throw new Qc401QueryException(400, $"{label} 條件長度或字元不正確。");
        return normalized;
    }

    private static void ValidatePage(int? requestedPage, int? requestedPageSize, out int page, out int pageSize)
    {
        page = requestedPage ?? 1;
        pageSize = requestedPageSize ?? DefaultPageSize;
        if (page < 1 || page > MaxPage)
            throw new Qc401QueryException(400, $"頁碼必須介於 1 到 {MaxPage:N0}。");
        if (pageSize < 1 || pageSize > MaxPageSize)
            throw new Qc401QueryException(400, $"每頁筆數必須介於 1 到 {MaxPageSize}。");
    }

    private sealed record NormalizedFilters(string? LotNo, string? CustomerNo, string? OpNo, string? MClassNo,
        string? CurGroupNo, string? ErfNo, string? MaterialNo, string? VendorNo, string? ProductNo);
}

public sealed class Qc401SpcErfUnreleasedQuery
{
    public string? LotNo { get; set; }
    public string? CustomerNo { get; set; }
    public string? OpNo { get; set; }
    public string? MClassNo { get; set; }
    public string? CurGroupNo { get; set; }
    public string? ErfNo { get; set; }
    public string? MaterialNo { get; set; }
    public string? VendorNo { get; set; }
    public string? ProductNo { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record Qc401SpcErfUnreleasedResponse(long TotalCount, int Page, int PageSize, int PageCount,
    IReadOnlyList<Qc401SpcErfUnreleasedRow> Items);

public sealed record Qc401SpcErfUnreleasedRow(
    string ErfNo, string LotNo, string CustomerLotNo, string ProductNo, string ProductName, int Status,
    string OpNo, string OpName, string CustomerNo, string CustomerName, string MClassNo,
    string MaterialNo, string MaterialName, string VendorNo, string VendorName, string HoldDescription,
    string CurGroupNo, string DispDescription, string RuleNo, decimal CreateQty, decimal ScrapQty,
    decimal CurQty, DateTime CreateDate, int Priority, string Creator, int ErfType, int ErfSource);

public sealed class Qc401QueryException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
