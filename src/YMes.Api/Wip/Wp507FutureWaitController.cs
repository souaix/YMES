using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/future-wait-release")]
public sealed class Wp507FutureWaitController(
    Wp507FutureWaitRepository repository,
    ILogger<Wp507FutureWaitController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Wp507FutureWaitQuery query, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        try
        {
            var page = await repository.ListAsync(query, cancellationToken);
            Response.Headers["X-Page"] = page.Page.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Response.Headers["X-Page-Size"] = page.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Response.Headers["X-Has-More"] = page.HasMore ? "true" : "false";
            return Ok(page.Items);
        }
        catch (Wp507FutureWaitRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "預先等待查詢條件不正確", detail: ex.Message);
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            HttpContext.Abort();
            return new EmptyResult();
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP507 future-wait read failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "預先等待查詢服務尚未就緒", detail: "TBLWIPFUTUREWAIT 結構或唯讀權限尚未就緒；未使用假資料。");
        }
    }

    [HttpGet("lookups/op")]
    public async Task<IActionResult> LookupOperations(
        [FromQuery] Wp507OpLookupQuery query, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        try
        {
            var page = await repository.LookupOperationsAsync(query, cancellationToken);
            Response.Headers["X-Page"] = page.Page.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Response.Headers["X-Page-Size"] = page.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Response.Headers["X-Has-More"] = page.HasMore ? "true" : "false";
            return Ok(page.Items);
        }
        catch (Wp507FutureWaitRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "作業站查找條件不正確", detail: ex.Message);
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            HttpContext.Abort();
            return new EmptyResult();
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP507 operation lookup failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "作業站查找服務尚未就緒", detail: "作業站主檔結構或唯讀權限尚未就緒；未使用假資料。");
        }
    }
}
