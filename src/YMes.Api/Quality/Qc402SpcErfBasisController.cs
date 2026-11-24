using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/spc/erfs")]
public sealed class Qc402SpcErfBasisController(NpgsqlDataSource dataSource) : ControllerBase
{
    private readonly Qc402SpcErfBasisRepository _repository = new(dataSource);

    [HttpGet("query")]
    public IActionResult Query([FromQuery] Qc402SpcErfBasisQuery query)
    {
        try
        {
            return Ok(_repository.Query(query));
        }
        catch (Qc402QueryException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "SC402 SPC 異常單查詢失敗", detail: ex.Message);
        }
        catch (PostgresException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "SC402 SPC 異常單查詢失敗",
                detail: "SC402 查詢結構或唯讀權限尚未就緒，請確認 migration 0070 與 YMES 查詢主檔。");
        }
    }
}

public sealed class Qc402SpcErfBasisRepository(NpgsqlDataSource dataSource)
{
    private const int DefaultPageSize = 100;
    private const int MaxPageSize = 100;
    private const int MaxPage = 100_000;

    private const string From = """
        FROM "YMES".tblwiperfbasis e
        LEFT JOIN "YMES".tblwiplotbasis lb
          ON lb.baselotno=COALESCE(NULLIF(e.baselotno,''),e.lotno)
        """;

    private const string Filter = """
        e.erfsource IN (1,2)
        AND (@lotNo IS NULL OR position(lower(@lotNo) in lower(e.lotno))>0
             OR EXISTS (SELECT 1 FROM "YMES".tblwiperfbatchlot b
                        WHERE b.erfno=e.erfno AND position(lower(@lotNo) in lower(b.lotno))>0))
        AND (@customerNo IS NULL OR lower(COALESCE(NULLIF(e.customerno,''),lb.customerno,''))=lower(@customerNo))
        AND (@opNo IS NULL OR lower(e.opno)=lower(@opNo))
        AND (@mClassNo IS NULL OR lower(e.mclassno)=lower(@mClassNo))
        AND (@curGroupNo IS NULL OR lower(e.curgroupno)=lower(@curGroupNo))
        AND (@erfNo IS NULL OR lower(e.erfno)=lower(@erfNo))
        """;

    public Qc402SpcErfBasisResponse Query(Qc402SpcErfBasisQuery query)
    {
        ValidatePage(query.Page, query.PageSize, out var page, out var pageSize);
        var filters = new NormalizedFilters(
            Normalize(query.LotNo, 50, "Lot No"),
            Normalize(query.CustomerNo, 50, "Customer No"),
            Normalize(query.OpNo, 20, "OP No"),
            Normalize(query.MClassNo, 30, "MClass No"),
            Normalize(query.CurGroupNo, 100, "Cur Group"),
            Normalize(query.ErfNo, 20, "ERF No"));

        if (filters.LotNo is null && filters.CustomerNo is null && filters.OpNo is null &&
            filters.MClassNo is null && filters.CurGroupNo is null && filters.ErfNo is null)
            throw new Qc402QueryException(400, "至少輸入一個有效查詢條件。");

        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);
        using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction))
            readOnly.ExecuteNonQuery();

        using var countCommand = new NpgsqlCommand($"SELECT count(*) {From} WHERE {Filter};", connection, transaction);
        AddFilters(countCommand, filters);
        var totalCount = (long)countCommand.ExecuteScalar()!;
        var pageCount = totalCount == 0 ? 0 : checked((int)Math.Ceiling(totalCount / (double)pageSize));
        var offset = checked((page - 1) * pageSize);

        using var command = new NpgsqlCommand($"""
            SELECT e.erfno,e.lotno,COALESCE(lb.customerlotno,''),
                   COALESCE(NULLIF(e.productno,''),lb.productno,''),e.status::int,e.opno,
                   COALESCE(NULLIF(e.customerno,''),lb.customerno,''),COALESCE(e.vendorno,''),
                   COALESCE(e.materialno,''),e.mclassno,COALESCE(e.holddescription,''),
                   e.curgroupno,COALESCE(e.dispdescription,''),COALESCE(e.ruleno,''),
                   COALESCE(e.createqty,0)::numeric,COALESCE(e.scrapqty,0)::numeric,
                   COALESCE(e.releaseqty,0)::numeric,COALESCE(e.releaser,''),e.releasedate,
                   GREATEST(0,FLOOR(EXTRACT(EPOCH FROM
                       ((CASE WHEN e.status IN (12,21) AND e.releasedate IS NOT NULL
                              THEN e.releasedate ELSE localtimestamp END)-e.createdate))/60))::bigint,
                   COALESCE(lb.priority,99)::int,COALESCE(e.qrsno,''),e.createdate,
                   e.erftype::int,e.erfsource::int,e.creator
            {From}
            WHERE {Filter}
            ORDER BY e.erfno COLLATE "C"
            LIMIT @limit OFFSET @offset;
            """, connection, transaction);
        AddFilters(command, filters);
        command.Parameters.Add("limit", NpgsqlDbType.Integer).Value = pageSize;
        command.Parameters.Add("offset", NpgsqlDbType.Integer).Value = offset;

        var rows = new List<Qc402SpcErfBasisRow>(pageSize);
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                rows.Add(new Qc402SpcErfBasisRow(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetInt32(4), reader.GetString(5), reader.GetString(6), reader.GetString(7),
                    reader.GetString(8), reader.GetString(9), reader.GetString(10), reader.GetString(11),
                    reader.GetString(12), reader.GetString(13), reader.GetDecimal(14), reader.GetDecimal(15),
                    reader.GetDecimal(16), reader.GetString(17), reader.IsDBNull(18) ? null : reader.GetDateTime(18),
                    reader.GetInt64(19), reader.GetInt32(20), reader.GetString(21), reader.GetDateTime(22),
                    reader.GetInt32(23), reader.GetInt32(24), reader.GetString(25)));
            }
        }

        transaction.Commit();
        return new Qc402SpcErfBasisResponse(totalCount, page, pageSize, pageCount, rows);
    }

    private static void AddFilters(NpgsqlCommand command, NormalizedFilters filters)
    {
        Add(command, "lotNo", filters.LotNo);
        Add(command, "customerNo", filters.CustomerNo);
        Add(command, "opNo", filters.OpNo);
        Add(command, "mClassNo", filters.MClassNo);
        Add(command, "curGroupNo", filters.CurGroupNo);
        Add(command, "erfNo", filters.ErfNo);
    }

    private static void Add(NpgsqlCommand command, string name, string? value) =>
        command.Parameters.Add(name, NpgsqlDbType.Varchar).Value = (object?)value ?? DBNull.Value;

    private static string? Normalize(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength || normalized.Any(char.IsControl))
            throw new Qc402QueryException(400, $"{label} 條件長度或字元不正確。");
        return normalized;
    }

    private static void ValidatePage(int? requestedPage, int? requestedPageSize, out int page, out int pageSize)
    {
        page = requestedPage ?? 1;
        pageSize = requestedPageSize ?? DefaultPageSize;
        if (page < 1 || page > MaxPage)
            throw new Qc402QueryException(400, $"頁碼必須介於 1 到 {MaxPage:N0}。");
        if (pageSize < 1 || pageSize > MaxPageSize)
            throw new Qc402QueryException(400, $"每頁筆數必須介於 1 到 {MaxPageSize}。");
    }

    private sealed record NormalizedFilters(string? LotNo, string? CustomerNo, string? OpNo,
        string? MClassNo, string? CurGroupNo, string? ErfNo);
}

public sealed class Qc402SpcErfBasisQuery
{
    public string? LotNo { get; set; }
    public string? CustomerNo { get; set; }
    public string? OpNo { get; set; }
    public string? MClassNo { get; set; }
    public string? CurGroupNo { get; set; }
    public string? ErfNo { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed record Qc402SpcErfBasisResponse(long TotalCount, int Page, int PageSize, int PageCount,
    IReadOnlyList<Qc402SpcErfBasisRow> Items);

public sealed record Qc402SpcErfBasisRow(
    string ErfNo, string LotNo, string CustomerLotNo, string ProductNo, int Status, string OpNo,
    string CustomerNo, string VendorNo, string MaterialNo, string MClassNo, string HoldDescription,
    string CurGroupNo, string DispDescription, string RuleNo, decimal CreateQty, decimal ScrapQty,
    decimal ReleaseQty, string Releaser, DateTime? ReleaseDate, long CalHoldTimeMinutes, int Priority, string QrsNo,
    DateTime CreateDate, int ErfType, int ErfSource, string Creator);

public sealed class Qc402QueryException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
