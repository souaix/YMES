using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Enterprise;

[ApiController]
[Authorize]
[Route("api/ent/forwarder-basis")]
public sealed class Et104Controller(Et104Repository repository, ILogger<Et104Controller> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? forwarderNo = null, [FromQuery] string? issueState = null) =>
        Execute(() => Ok(new { items = repository.List(forwarderNo, issueState) }));
    [HttpGet("item")]
    public IActionResult Item([FromQuery] string forwarderNo) => Execute(() => Ok(repository.Get(forwarderNo)));
    [HttpPost]
    public IActionResult Create([FromBody] Et104WriteRequest request) => Execute(() => Ok(repository.Create(request, Actor)));
    [HttpPut("item")]
    public IActionResult Update([FromQuery] string forwarderNo, [FromBody] Et104UpdateRequest request) =>
        Execute(() => Ok(repository.Update(forwarderNo, request, Actor)));
    [HttpPost("approve")]
    public IActionResult Approve([FromQuery] string forwarderNo, [FromBody] Et104ExpectedRequest request) =>
        Execute(() => Ok(repository.Approve(forwarderNo, request, Actor)));
    [HttpDelete("item")]
    public IActionResult Delete([FromQuery] string forwarderNo, [FromBody] Et104ExpectedRequest request) => Execute(() =>
    {
        var result = repository.Delete(forwarderNo, request, Actor);
        return result.Deleted ? NoContent() : Ok(result.Item);
    });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Et104RuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "貨運行操作失敗", detail: ex.Message); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Problem(statusCode: 409, title: "貨運行資料重複", detail: "貨運行識別碼已存在，請重新查詢。"); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("ET104 database operation failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: 503, title: "貨運行服務暫時無法使用",
                detail: "無法存取必要的貨運行或引用來源，請確認結構與必要權限已部署。");
        }
    }
}
