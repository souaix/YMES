using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/equipment-consume-status-logs")]
public sealed class Wp304EquipmentConsumeLogController(
    Wp304EquipmentConsumeLogRepository repository,
    Wp304EquipmentConsumeLookupRepository lookupRepository,
    ILogger<Wp304EquipmentConsumeLogController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Query(
        [FromQuery] Wp304EquipmentConsumeLogQuery query, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        return await ExecuteAsync(async () =>
        {
            var page = await repository.GetAsync(query, cancellationToken);
            SetPageHeaders(page.Page, page.PageSize, page.HasMore);
            return Ok(page.Items);
        });
    }

    [HttpGet("lookups/equipment")]
    public async Task<IActionResult> EquipmentLookup(
        [FromQuery] Wp304EquipmentConsumeLookupQuery query, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        return await ExecuteAsync(async () =>
        {
            var page = await lookupRepository.EquipmentAsync(query, cancellationToken);
            SetPageHeaders(page.Page, page.PageSize, page.HasMore);
            return Ok(page.Items);
        });
    }

    [HttpGet("lookups/consumables")]
    public async Task<IActionResult> ConsumableLookup(
        [FromQuery] Wp304EquipmentConsumeLookupQuery query, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        return await ExecuteAsync(async () =>
        {
            var page = await lookupRepository.ConsumablesAsync(query, cancellationToken);
            SetPageHeaders(page.Page, page.PageSize, page.HasMore);
            return Ok(page.Items);
        });
    }

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (Wp304EquipmentConsumeLogRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "WP304 設備耗品歷程查詢失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP304 equipment consumable read failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP304 設備耗品歷程服務尚未就緒",
                detail: "歷程或查找來源表、唯讀資料庫權限尚未就緒；未使用假資料或替代資料。");
        }
    }

    private void SetPageHeaders(int page, int pageSize, bool hasMore)
    {
        Response.Headers["X-Page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Page-Size"] = pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Has-More"] = hasMore ? "true" : "false";
    }
}
