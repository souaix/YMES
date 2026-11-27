using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/areas/{areaNo}/operators")]
public sealed class Wp307AreaOperatorController(
    Wp307AreaOperatorRepository repository,
    ILogger<Wp307AreaOperatorController> logger) : ControllerBase
{
    [HttpGet("filter-options")]
    public async Task<IActionResult> FilterOptions(
        [FromRoute] string areaNo, [FromQuery] Wp307FilterOptionsQuery query, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        return await ExecuteAsync(async () => Ok(await repository.GetFilterOptionsAsync(areaNo, query, cancellationToken)));
    }

    [HttpGet("eligible")]
    public async Task<IActionResult> Eligible(
        [FromRoute] string areaNo, [FromQuery] string? userNo, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        return await ExecuteAsync(async () => Ok(await repository.CheckEligibleAsync(areaNo, userNo, cancellationToken)));
    }

    [HttpGet("current")]
    public async Task<IActionResult> Current(
        [FromRoute] string areaNo, [FromQuery] Wp307CurrentQuery query, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        return await ExecuteAsync(async () =>
        {
            var page = await repository.GetCurrentAsync(areaNo, query, cancellationToken);
            SetPageHeaders(page.Page, page.PageSize, page.HasMore);
            return Ok(page.Items);
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(
        [FromRoute] string areaNo, [FromQuery] Wp307HistoryQuery query, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        return await ExecuteAsync(async () =>
        {
            var page = await repository.GetHistoryAsync(areaNo, query, cancellationToken);
            SetPageHeaders(page.Page, page.PageSize, page.HasMore);
            return Ok(page.Items);
        });
    }

    private void SetPageHeaders(int page, int pageSize, bool hasMore)
    {
        Response.Headers["X-Page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Page-Size"] = pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Has-More"] = hasMore ? "true" : "false";
    }

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (Wp307AreaOperatorRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "WP307 區域作業人員查詢失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP307 operator read failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP307 區域作業人員服務尚未就緒",
                detail: "區域作業人員來源表或唯讀資料庫權限尚未就緒；未使用假資料或替代資料。");
        }
    }
}
