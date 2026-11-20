using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Enterprise;

[ApiController]
[Authorize]
[Route("api/ent/broker-basis")]
public sealed class Et103Controller(Et103Repository repository, ILogger<Et103Controller> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? brokerNo = null, [FromQuery] string? issueState = null) =>
        Execute(() => Ok(new { items = repository.List(brokerNo, issueState) }));
    [HttpGet("item")]
    public IActionResult Item([FromQuery] string brokerNo) => Execute(() => Ok(repository.Get(brokerNo)));
    [HttpPost]
    public IActionResult Create([FromBody] Et103WriteRequest request) => Execute(() => Ok(repository.Create(request, Actor)));
    [HttpPut("item")]
    public IActionResult Update([FromQuery] string brokerNo, [FromBody] Et103UpdateRequest request) =>
        Execute(() => Ok(repository.Update(brokerNo, request, Actor)));
    [HttpPost("approve")]
    public IActionResult Approve([FromQuery] string brokerNo, [FromBody] Et103ExpectedRequest request) =>
        Execute(() => Ok(repository.Approve(brokerNo, request, Actor)));
    [HttpDelete("item")]
    public IActionResult Delete([FromQuery] string brokerNo, [FromBody] Et103ExpectedRequest request) => Execute(() =>
    {
        var result = repository.Delete(brokerNo, request, Actor);
        return result.Deleted ? NoContent() : Ok(result.Item);
    });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Et103RuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "報關行操作失敗", detail: ex.Message); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Problem(statusCode: 409, title: "報關行資料重複", detail: "報關行識別碼已存在，請重新查詢。"); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("ET103 database operation failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: 503, title: "報關行服務暫時無法使用",
                detail: "無法存取必要的報關行或引用來源，請確認結構與必要權限已部署。");
        }
    }
}
