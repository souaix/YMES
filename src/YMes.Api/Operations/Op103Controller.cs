using System.Security.Claims;
using MES.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Operations;

[ApiController]
[Authorize]
[Route("api/operation-types")]
public sealed class Op103Controller(NpgsqlDataSource dataSource, ILogger<Op103Controller> logger) : ControllerBase
{
    private readonly Op103Repository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";
    [HttpGet]
    public IActionResult List([FromQuery] int? issueState, [FromQuery] string? opType) => Execute(() => Ok(repository.List(issueState, opType)));
    [HttpGet("item")]
    public IActionResult Get([FromQuery] string opType) => Execute(() => Ok(repository.Get(opType)));
    [HttpPost]
    public IActionResult Create([FromBody] Op103WriteRequest request) => Execute(() => Written("Create", repository.Create(request, Actor)));
    [HttpPut("item")]
    public IActionResult Update([FromQuery] string opType, [FromBody] Op103UpdateRequest request) => Execute(() => Written("Update", repository.Update(opType, request, Actor)));
    [HttpPost("approve")]
    public IActionResult Approve([FromQuery] string opType, [FromBody] Op103CommandRequest request) => Execute(() => Written("Approve", repository.Approve(opType, request, Actor)));
    [HttpDelete("item")]
    public IActionResult Delete([FromQuery] string opType, [FromBody] Op103CommandRequest request) => Execute(() =>
    {
        var result = repository.Delete(opType, request, Actor);
        logger.LogInformation("OP103 {Action} {OpType} by {Actor}", result.Outcome, result.Item.OpType, Actor);
        return Ok(result);
    });
    private IActionResult Written(string action, Op103TypeDto row)
    { logger.LogInformation("OP103 {Action} {OpType} by {Actor}; state {State}, stamp {Stamp}", action, row.OpType, Actor, row.IssueState, row.DataStamp); return Ok(row); }
    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (MasterDataRuleException ex) { return Problem(statusCode: ex.StatusCode, title: "作業站類別操作失敗", detail: ex.Message); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Problem(statusCode: 409, title: "作業站類別重複", detail: "編號已存在，請重新查詢。"); }
        catch (NpgsqlException ex)
        {
            logger.LogError("OP103 database failed for {Actor}: {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: 503, title: "作業站類別服務暫時無法使用", detail: "無法存取作業站類別、主檔或共用屬性，請重新查詢確認資料。");
        }
    }
}
