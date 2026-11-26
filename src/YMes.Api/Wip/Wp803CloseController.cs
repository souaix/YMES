using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/8d-close")]
public sealed class Wp803CloseController(Wp803CloseRepository repository,
    ILogger<Wp803CloseController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult List([FromQuery] Wp803CloseQuery query) => Execute(() => Ok(repository.List(query)));

    [HttpGet("{qrsNo}")]
    public IActionResult Detail(string qrsNo) => Execute(() => Ok(repository.Detail(qrsNo)));

    [HttpGet("{qrsNo}/team-members")]
    public IActionResult TeamMembers(string qrsNo, [FromQuery] string? departmentNo) =>
        Execute(() => Ok(repository.TeamMembers(qrsNo, departmentNo)));

    [HttpPut("{qrsNo}")]
    public IActionResult Update(string qrsNo, [FromBody] Wp803CloseUpdateRequest? request) =>
        Execute(() => Ok(repository.Update(qrsNo, request)));

    [HttpPost("approve")]
    public IActionResult Approve()
    {
        Response.Headers["Cache-Control"] = "no-store";
        return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "WP803 簽核來源尚未就緒",
            detail: "MES 的 8D CLOSE Issue Job Map／簽核回呼規則尚未核實；未變更 Release、ReleaseLog 或 8D 主單。");
    }

    private IActionResult Execute(Func<IActionResult> action)
    {
        Response.Headers["Cache-Control"] = "no-store";
        try { return action(); }
        catch (Wp803CloseRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "WP803 8D 結案查詢失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP803 read failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "WP803 8D 結案查詢服務尚未就緒",
                detail: "必要的 8D 結案來源表或唯讀權限尚未就緒；未使用假資料或替代資料。");
        }
    }
}
