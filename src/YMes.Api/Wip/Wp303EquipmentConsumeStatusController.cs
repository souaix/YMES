using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/equipment-consume-status")]
public sealed class Wp303EquipmentConsumeStatusController(
    Wp303EquipmentConsumeStatusRepository repository,
    ILogger<Wp303EquipmentConsumeStatusController> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public Task<IActionResult> List([FromQuery] Wp303EquipmentConsumeStatusQuery query, CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var page = await repository.ListAsync(query, token);
            SetPageHeaders(page.Page, page.PageSize, page.HasMore, page.TotalCount);
            return page.Items;
        });

    [HttpGet("lookups/equipment")]
    public Task<IActionResult> EquipmentLookup(
        [FromQuery] Wp303EquipmentConsumeStatusLookupQuery query, CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var page = await repository.EquipmentLookupAsync(query, token);
            SetPageHeaders(page.Page, page.PageSize, page.HasMore);
            return page.Items;
        });

    [HttpGet("lookups/consumables")]
    public Task<IActionResult> ConsumableLookup(
        [FromQuery] Wp303EquipmentConsumeBasisLookupQuery query, CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var page = await repository.ConsumableLookupAsync(query, token);
            SetPageHeaders(page.Page, page.PageSize, page.HasMore);
            return page.Items;
        });

    [HttpPost]
    public Task<IActionResult> Create(
        [FromBody] Wp303EquipmentConsumeStatusAddRequest request, CancellationToken token) =>
        ExecuteAsync(() => repository.CreateAsync(request, Actor, token));

    [HttpPut("item")]
    public Task<IActionResult> Update(
        [FromBody] Wp303EquipmentConsumeStatusEditRequest request, CancellationToken token) =>
        ExecuteAsync(() => repository.UpdateAsync(request, Actor, token));

    [HttpPost("delete")]
    public Task<IActionResult> DeleteMany(
        [FromBody] Wp303EquipmentConsumeStatusDeleteRequest request, CancellationToken token) =>
        ExecuteAsync(() => repository.DeleteManyAsync(request, token));

    [HttpPost("reset")]
    public Task<IActionResult> Reset(
        [FromBody] Wp303EquipmentConsumeStatusResetRequest request, CancellationToken token) =>
        ExecuteAsync(() => repository.ResetAsync(request, Actor, token));

    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        Response.Headers.CacheControl = "no-store";
        try { return Ok(await action()); }
        catch (Wp303EquipmentConsumeStatusRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "WP303 設備耗品現況操作失敗", detail: ex.Message);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "設備耗品現況或歷程序號重複", detail: "本次交易已回滾；請重新查詢後再操作。");
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            HttpContext.Abort();
            return new EmptyResult();
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP303 equipment consumable operation failed ({ErrorType}) for {Actor}",
                ex.GetType().Name, Actor);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP303 設備耗品現況服務尚未就緒",
                detail: "資料表或資料庫權限尚未就緒；請重新查詢確認，未使用假資料。");
        }
    }

    private void SetPageHeaders(int page, int pageSize, bool hasMore, long? totalCount = null)
    {
        Response.Headers["X-Page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Page-Size"] = pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Has-More"] = hasMore ? "true" : "false";
        if (totalCount.HasValue)
            Response.Headers["X-Total-Count"] = totalCount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
