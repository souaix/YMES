using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/equipment-qc-list-logs")]
public sealed class Wp306EquipmentQcListController(
    Wp306EquipmentQcListRepository repository,
    ILogger<Wp306EquipmentQcListController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Headers(
        [FromQuery] Wp306EquipmentQcListQuery query, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        return await ExecuteAsync(async () =>
        {
            var page = await repository.GetHeadersAsync(query, cancellationToken);
            Response.Headers["X-Page"] = page.Page.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Response.Headers["X-Page-Size"] = page.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Response.Headers["X-Has-More"] = page.HasMore ? "true" : "false";
            return Ok(page.Items);
        });
    }

    [HttpGet("{qcListSerial}/details")]
    public async Task<IActionResult> Details(
        [FromRoute] string qcListSerial, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        return await ExecuteAsync(async () =>
            Ok(await repository.GetDetailsAsync(qcListSerial, cancellationToken)));
    }

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (Wp306EquipmentQcListRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "WP306 設備點檢歷程查詢失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP306 inspection history read failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP306 設備點檢歷程服務尚未就緒",
                detail: "點檢歷程來源表或唯讀資料庫權限尚未就緒；未使用假資料或替代資料。" );
        }
    }
}
