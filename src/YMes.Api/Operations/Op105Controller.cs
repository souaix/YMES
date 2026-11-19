using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Operations;

[ApiController]
[Authorize]
[Route("api/operation-groups")]
public sealed class Op105Controller(Op105Repository repository, ILogger<Op105Controller> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? opGroupNo, [FromQuery] string? psNo) => Execute(() => Ok(new { items = repository.List(opGroupNo, psNo) }));
    [HttpGet("item")]
    public IActionResult Item([FromQuery] string opGroupNo) => Execute(() => Ok(repository.Get(opGroupNo)));
    [HttpPost]
    public IActionResult Create([FromBody] Op105GroupWriteRequest request) => Execute(() => Ok(repository.Create(request, Actor)));
    [HttpPut("item")]
    public IActionResult Update([FromQuery] string opGroupNo, [FromBody] Op105GroupUpdateRequest request) => Execute(() => Ok(repository.Update(opGroupNo, request, Actor)));
    [HttpDelete("item")]
    public IActionResult Delete([FromQuery] string opGroupNo, [FromBody] Op105ExpectedRequest request) => Execute(() =>
    {
        repository.Delete(opGroupNo, request, Actor);
        return NoContent();
    });
    [HttpGet("members")]
    public IActionResult Members([FromQuery] string opGroupNo) => Execute(() => Ok(repository.Members(opGroupNo)));
    [HttpPost("members")]
    public IActionResult AddMember([FromQuery] string opGroupNo, [FromBody] Op105MemberCreateRequest request) => Execute(() => Ok(repository.AddMember(opGroupNo, request, Actor)));
    [HttpPut("members")]
    public IActionResult UpdateMember([FromQuery] string opGroupNo, [FromQuery] string opNo, [FromBody] Op105MemberUpdateRequest request) => Execute(() => Ok(repository.UpdateMember(opGroupNo, opNo, request, Actor)));
    [HttpDelete("members")]
    public IActionResult DeleteMember([FromQuery] string opGroupNo, [FromQuery] string opNo, [FromBody] Op105ExpectedRequest request) => Execute(() => Ok(repository.DeleteMember(opGroupNo, opNo, request, Actor)));
    [HttpGet("lookups/production-segments")]
    public IActionResult Segments() => Execute(() => Ok(new { items = repository.Segments() }));
    [HttpGet("lookups/operations")]
    public IActionResult Operations([FromQuery] string? psNo, [FromQuery] int issueState = 2) => Execute(() => Ok(new { items = repository.Operations(psNo, issueState) }));

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Op105RuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "作業站群組操作失敗", detail: ex.Message);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Problem(statusCode: 409, title: "作業站群組資料重複", detail: "群組或成員識別碼已存在，請重新查詢。");
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("OP105 database operation failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: 503, title: "作業站群組服務暫時無法使用", detail: "目前無法存取作業站群組及相依資料，請確認資料庫與必要權限已部署後重試。");
        }
    }
}
