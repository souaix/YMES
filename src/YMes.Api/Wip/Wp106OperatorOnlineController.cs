using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/operator-online")]
public sealed class Wp106OperatorOnlineController(
    Wp106OperatorOnlineRepository repository,
    ILogger<Wp106OperatorOnlineController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Wp106OperatorOnlineQuery query, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        try { return Ok(await repository.ListAsync(query, cancellationToken)); }
        catch (Wp106OperatorOnlineRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "WP106 上工現況查詢失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP106 online-state read failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP106 上工現況服務尚未就緒",
                detail: "正式上工現況來源表或唯讀資料庫權限尚未就緒；未使用假資料或替代資料。");
        }
    }
}
