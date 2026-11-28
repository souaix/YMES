using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/ipqc")]
public sealed class Wp206IpqcController(
    Wp206IpqcRepository repository,
    ILogger<Wp206IpqcController> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet("lookups/operations")]
    public async Task<IActionResult> Operations([FromQuery] Wp206OperationQuery query, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        try
        {
            var page = await repository.GetOperationsAsync(query, token);
            Response.Headers["X-Page"] = page.Page.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Response.Headers["X-Page-Size"] = page.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Response.Headers["X-Has-More"] = page.HasMore ? "true" : "false";
            return Ok(page.Items);
        }
        catch (Wp206IpqcRuleException ex) { return RuleProblem(ex, "WP206 作業站查詢失敗"); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            HttpContext.Abort();
            return new EmptyResult();
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP206 operation lookup failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "IPQC 作業站查詢尚未就緒",
                detail: "作業站來源表或唯讀權限尚未就緒；未使用假資料。");
        }
    }

    [HttpGet("lookups/operations/{opNo}/abnormalities")]
    public async Task<IActionResult> Abnormalities([FromRoute] string opNo, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        try { return Ok(await repository.GetAbnormalitiesAsync(opNo, token)); }
        catch (Wp206IpqcRuleException ex) { return RuleProblem(ex, "WP206 異常項目查詢失敗"); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            HttpContext.Abort();
            return new EmptyResult();
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP206 abnormal lookup failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "IPQC 異常項目查詢尚未就緒",
                detail: "作業站異常關聯、異常主檔或位置來源表／唯讀權限尚未就緒；未使用假資料。");
        }
    }

    [HttpPost("abnormal-records")]
    public async Task<IActionResult> Create(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] Wp206RecordRequest request,
        CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        try
        {
            var result = await repository.CreateAsync(idempotencyKey, request, Actor, token);
            return Ok(result);
        }
        catch (Wp206IpqcRuleException ex) { return RuleProblem(ex, "WP206 不良記錄寫入失敗"); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "IPQC 請求識別碼衝突", detail: "本次交易已回滾；請重新載入畫面後再提交。");
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            HttpContext.Abort();
            return new EmptyResult();
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP206 record write failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "IPQC 寫入服務尚未就緒",
                detail: "IPQC 來源表或資料庫權限尚未就緒；交易未使用假資料。");
        }
    }

    private ObjectResult RuleProblem(Wp206IpqcRuleException ex, string title) =>
        Problem(statusCode: ex.StatusCode, title: title, detail: ex.Message);
}
