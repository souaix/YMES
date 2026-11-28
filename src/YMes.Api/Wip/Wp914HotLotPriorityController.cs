using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/hotlot-priority")]
public sealed class Wp914HotLotPriorityController(
    Wp914HotLotPriorityRepository repository,
    ILogger<Wp914HotLotPriorityController> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet("lots")]
    public async Task<IActionResult> Lots([FromQuery] Wp914LotQuery query, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        try
        {
            var page = await repository.GetLotsAsync(query, token);
            SetPageHeaders(page.Page, page.PageSize, page.HasMore);
            return Ok(page.Items);
        }
        catch (Wp914RuleException ex) { return RuleProblem(ex, "WP914 生產批查詢失敗"); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            HttpContext.Abort();
            return new EmptyResult();
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP914 lot query failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP914 查詢服務尚未就緒", detail: "生產批來源表或唯讀權限尚未就緒；未使用假資料。");
        }
    }

    [HttpGet("lookups/{field}")]
    public async Task<IActionResult> Lookup(
        [FromRoute] string field, [FromQuery] Wp914LookupQuery query, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        try
        {
            var page = await repository.GetLookupAsync(field, query, token);
            SetPageHeaders(page.Page, page.PageSize, page.HasMore);
            return Ok(page.Items);
        }
        catch (Wp914RuleException ex) { return RuleProblem(ex, "WP914 查找失敗"); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            HttpContext.Abort();
            return new EmptyResult();
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP914 lookup failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP914 查找服務尚未就緒", detail: "生產批來源表或唯讀權限尚未就緒；未使用假資料。");
        }
    }

    [HttpPost("set")]
    public async Task<IActionResult> Set(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] Wp914SetRequest request,
        CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        try { return Ok(await repository.SetAsync(idempotencyKey, request, Actor, token)); }
        catch (Wp914RuleException ex) { return RuleProblem(ex, "WP914 生產批設定失敗"); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            HttpContext.Abort();
            return new EmptyResult();
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP914 priority update failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP914 設定服務尚未就緒", detail: "生產批來源表或資料庫權限尚未就緒；交易未使用假資料。");
        }
    }

    private void SetPageHeaders(int page, int pageSize, bool hasMore)
    {
        Response.Headers["X-Page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Page-Size"] = pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Has-More"] = hasMore ? "true" : "false";
    }

    private ObjectResult RuleProblem(Wp914RuleException ex, string title) =>
        Problem(statusCode: ex.StatusCode, title: title, detail: ex.Message);
}
