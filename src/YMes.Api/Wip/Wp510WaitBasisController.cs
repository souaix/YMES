using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/wait-basis")]
public sealed class Wp510WaitBasisController(
    Wp510WaitBasisRepository repository,
    ILogger<Wp510WaitBasisController> logger) : ControllerBase
{
    [HttpGet("waits")]
    public async Task<IActionResult> Waits([FromQuery] Wp510WaitBasisQuery query, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        try
        {
            var page = await repository.GetWaitsAsync(query, cancellationToken);
            SetPageHeaders(page.Page, page.PageSize, page.HasMore);
            return Ok(page.Items);
        }
        catch (Wp510RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "WP510 等待單查詢失敗", detail: ex.Message); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        { HttpContext.Abort(); return new EmptyResult(); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP510 wait query failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP510 等待單查詢服務尚未就緒", detail: "WaitBasis 來源表或唯讀權限尚未就緒；未使用假資料。");
        }
    }

    [HttpGet("lookups/{field}")]
    public async Task<IActionResult> Lookup(
        [FromRoute] string field, [FromQuery] Wp510LookupQuery query, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        try
        {
            var page = await repository.GetLookupAsync(field, query, cancellationToken);
            SetPageHeaders(page.Page, page.PageSize, page.HasMore);
            return Ok(page.Items);
        }
        catch (Wp510RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "WP510 查找失敗", detail: ex.Message); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        { HttpContext.Abort(); return new EmptyResult(); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP510 lookup failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP510 查找服務尚未就緒", detail: "WaitBasis 來源表或唯讀權限尚未就緒；未使用假資料。");
        }
    }

    [HttpGet("waits/{waitNo}/dispositions")]
    public async Task<IActionResult> Dispositions(
        [FromRoute] string waitNo,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        try
        {
            var result = await repository.GetDispositionsAsync(waitNo, page, pageSize, cancellationToken);
            SetPageHeaders(result.Page, result.PageSize, result.HasMore);
            return Ok(result.Items);
        }
        catch (Wp510RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "WP510 Wait Info 查詢失敗", detail: ex.Message); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        { HttpContext.Abort(); return new EmptyResult(); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP510 disposition query failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP510 Wait Info 服務尚未就緒", detail: "Wait Disposition 或 NodeBasis 來源表／唯讀權限尚未就緒；未使用假資料。");
        }
    }

    private void SetPageHeaders(int page, int pageSize, bool hasMore)
    {
        Response.Headers["X-Page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Page-Size"] = pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Has-More"] = hasMore ? "true" : "false";
    }
}
