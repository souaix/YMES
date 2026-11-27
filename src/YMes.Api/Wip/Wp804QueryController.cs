using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/8d-query")]
public sealed class Wp804QueryController(Wp804QueryRepository repository,
    ILogger<Wp804QueryController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult List([FromQuery] Wp804Query query)
    {
        Response.Headers["Cache-Control"] = "no-store";
        try { return Ok(repository.List(query)); }
        catch (Wp804QueryRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "WP804 8D 查詢失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP804 read failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP804 8D 查詢服務尚未就緒",
                detail: "8D 主表或唯讀資料庫權限尚未就緒；未使用假資料或替代資料。");
        }
    }
}
