using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Enterprise;

[ApiController]
[Authorize]
[Route("api/ent/taker-basis")]
public sealed class Et105Controller(Et105Repository repository, ILogger<Et105Controller> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? takerNo = null, [FromQuery] string? issueState = null) =>
        Execute(() => Ok(new { items = repository.List(takerNo, issueState) }));
    [HttpGet("item")]
    public IActionResult Item([FromQuery] string takerNo) => Execute(() => Ok(repository.Get(takerNo)));
    [HttpPost]
    public IActionResult Create([FromBody] Et105WriteRequest request) => Execute(() => Ok(repository.Create(request, Actor)));
    [HttpPut("item")]
    public IActionResult Update([FromQuery] string takerNo, [FromBody] Et105UpdateRequest request) =>
        Execute(() => Ok(repository.Update(takerNo, request, Actor)));
    [HttpPost("approve")]
    public IActionResult Approve([FromQuery] string takerNo, [FromBody] Et105ExpectedRequest request) =>
        Execute(() => Ok(repository.Approve(takerNo, request, Actor)));
    [HttpDelete("item")]
    public IActionResult Delete([FromQuery] string takerNo, [FromBody] Et105ExpectedRequest request) => Execute(() =>
    {
        var result = repository.Delete(takerNo, request, Actor);
        return result.Deleted ? NoContent() : Ok(result.Item);
    });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Et105RuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "取貨公司操作失敗", detail: ex.Message); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Problem(statusCode: 409, title: "取貨公司資料重複", detail: "取貨公司識別碼已存在，請重新查詢。"); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("ET105 database operation failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: 503, title: "取貨公司服務暫時無法使用",
                detail: "無法存取必要的取貨公司或引用來源，請確認結構與必要權限已部署。");
        }
    }
}
