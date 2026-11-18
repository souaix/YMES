using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Processes;

[ApiController]
[Authorize]
[Route("api/fab-prs-stages")]
public sealed class Ps102Controller(NpgsqlDataSource dataSource, ILogger<Ps102Controller> logger) : ControllerBase
{
    private readonly Ps102Repository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? stageNo = null) =>
        Execute(() => Ok(new { items = repository.List(stageNo) }));

    [HttpPost]
    public IActionResult Create([FromBody] Ps102StageWriteRequest request) =>
        Execute(() => Ok(repository.Create(request, Actor)));

    [HttpPut("{stageNo}")]
    public IActionResult Update(string stageNo, [FromBody] Ps102StageUpdateRequest request) =>
        Execute(() => Ok(repository.Update(stageNo, request, Actor)));

    [HttpDelete("{stageNo}")]
    public IActionResult Delete(string stageNo, [FromBody] Ps102StageStateRequest request) =>
        Execute(() => Ok(repository.Delete(stageNo, request, Actor)));

    [HttpPost("{stageNo}/approve")]
    public IActionResult Approve(string stageNo, [FromBody] Ps102StageStateRequest request) =>
        Execute(() => Ok(repository.Approve(stageNo, request, Actor)));

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Ps102RuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "製造層別操作失敗", detail: ex.Message);
        }
        catch (PostgresException ex)
        {
            logger.LogError("PS102 PostgreSQL operation failed ({SqlState}) for {Actor}", ex.SqlState, Actor);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "製造層別資料操作失敗", detail: "YMES 資料庫操作失敗，請確認製造層別資料表與必要資源已部署。");
        }
        catch (NpgsqlException)
        {
            logger.LogError("PS102 database connection unavailable for {Actor}", Actor);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "製造層別服務暫時無法使用", detail: "目前無法連線至 YMES 資料庫，請稍後再試。");
        }
    }
}
