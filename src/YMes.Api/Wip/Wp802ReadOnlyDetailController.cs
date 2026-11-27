using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/8d")]
public sealed class Wp802ReadOnlyDetailController(Wp802ReadOnlyDetailRepository repository,
    ILogger<Wp802ReadOnlyDetailController> logger) : ControllerBase
{
    [HttpGet("{qrsNo}/detail")]
    public IActionResult Detail(string qrsNo)
    {
        Response.Headers["Cache-Control"] = "no-store";
        try { return Ok(repository.Detail(qrsNo)); }
        catch (Wp802ReadOnlyDetailRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "WP802 8D 唯讀明細查詢失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP802 detail read failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP802 8D 唯讀明細服務尚未就緒",
                detail: "8D 子表或唯讀資料庫權限尚未就緒；未使用假資料或替代資料。");
        }
    }
}
