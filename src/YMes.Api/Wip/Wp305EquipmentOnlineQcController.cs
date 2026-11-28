using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/equipment-online-qc")]
public sealed class Wp305EquipmentOnlineQcController(
    Wp305EquipmentOnlineQcRepository repository,
    ILogger<Wp305EquipmentOnlineQcController> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet("lookups/equipment-types")]
    public Task<IActionResult> EquipmentTypes([FromQuery] Wp305EquipmentTypeQuery query, CancellationToken token) =>
        ExecuteAsync(() => repository.GetEquipmentTypesAsync(query, token));

    [HttpGet("lookups/equipment")]
    public Task<IActionResult> Equipment([FromQuery] Wp305EquipmentLookupQuery query, CancellationToken token) =>
        ExecutePageAsync(() => repository.GetEquipmentAsync(query, token));

    [HttpGet("candidates")]
    public Task<IActionResult> Candidates([FromQuery] Wp305CandidateQuery query, CancellationToken token) =>
        ExecuteAsync(() => repository.GetCandidatesAsync(query, token));

    [HttpGet("lists/{qcListNo}")]
    public Task<IActionResult> QcList(
        [FromRoute] string qcListNo, [FromQuery] Wp305QcListQuery query, CancellationToken token) =>
        ExecuteAsync(() => repository.GetQcListAsync(qcListNo, query, token));

    [HttpPost("records")]
    public Task<IActionResult> Create(
        [FromBody] Wp305InspectionSaveRequest request, CancellationToken token) =>
        ExecuteAsync(() => repository.CreateAsync(request, Actor, token));

    private async Task<IActionResult> ExecutePageAsync<T>(Func<Task<Wp305Page<T>>> action)
    {
        Response.Headers.CacheControl = "no-store";
        try
        {
            var page = await action();
            Response.Headers["X-Page"] = page.Page.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Response.Headers["X-Page-Size"] = page.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Response.Headers["X-Has-More"] = page.HasMore ? "true" : "false";
            return Ok(page.Items);
        }
        catch (Wp305EquipmentOnlineQcRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "WP305 設備點檢查詢失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP305 equipment lookup failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP305 設備點檢服務尚未就緒",
                detail: "設備主檔或 WP305 查詢來源表／唯讀權限尚未就緒；未使用假資料。");
        }
    }

    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        Response.Headers.CacheControl = "no-store";
        try { return Ok(await action()); }
        catch (Wp305EquipmentOnlineQcRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "WP305 設備點檢操作失敗", detail: ex.Message); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "WP305 點檢序號衝突", detail: "本次交易已回滾；請重新載入候檢任務後再試。");
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            HttpContext.Abort();
            return new EmptyResult();
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP305 equipment QC operation failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP305 設備點檢服務尚未就緒",
                detail: "點檢來源表或資料庫權限尚未就緒；交易未使用假資料。");
        }
    }
}
