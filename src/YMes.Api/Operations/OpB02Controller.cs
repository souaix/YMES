using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Operations;

[ApiController]
[Authorize]
[Route("api/ecn-operation-groups")]
public sealed class OpB02Controller(OpB02Repository repository, ILogger<OpB02Controller> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? issueState = "all", [FromQuery] string? opGroupNo = null) =>
        Execute(() => Ok(new { items = repository.List(issueState, opGroupNo) }));
    [HttpGet("item")]
    public IActionResult Item([FromQuery] string opGroupNo) => Execute(() => Ok(repository.Get(opGroupNo)));
    [HttpPost]
    public IActionResult Create([FromBody] OpB02GroupWriteRequest request) => Execute(() => Ok(repository.Create(request, Actor)));
    [HttpPut("item")]
    public IActionResult Update([FromQuery] string opGroupNo, [FromBody] OpB02GroupUpdateRequest request) =>
        Execute(() => Ok(repository.Update(opGroupNo, request, Actor)));
    [HttpDelete("item")]
    public IActionResult Delete([FromQuery] string opGroupNo, [FromBody] OpB02ExpectedRequest request) =>
        Execute(() => Ok(repository.Delete(opGroupNo, request, Actor)));
    [HttpPost("approve")]
    public IActionResult Approve([FromQuery] string opGroupNo, [FromBody] OpB02ExpectedRequest request) =>
        Execute(() => Ok(repository.Approve(opGroupNo, request, Actor)));

    [HttpGet("operations")]
    public IActionResult Operations([FromQuery] string opGroupNo) => Execute(() => Ok(repository.Operations(opGroupNo)));
    [HttpPost("operations")]
    public IActionResult AddOperation([FromQuery] string opGroupNo, [FromBody] OpB02OperationCreateRequest request) =>
        Execute(() => Ok(repository.AddOperation(opGroupNo, request, Actor)));
    [HttpDelete("operations")]
    public IActionResult DeleteOperation([FromQuery] string opGroupNo, [FromQuery] string opNo,
        [FromBody] OpB02ExpectedRequest request) => Execute(() => Ok(repository.DeleteOperation(opGroupNo, opNo, request, Actor)));

    [HttpGet("lookups/production-segments")]
    public IActionResult Segments() => Execute(() => Ok(new { items = repository.Segments() }));
    [HttpGet("lookups/operations")]
    public IActionResult OperationChoices([FromQuery] string psNo, [FromQuery] int issueState = 2,
        [FromQuery] bool notInAnyGroup = true) => Execute(() => Ok(new { items = repository.OperationChoices(psNo, issueState, notInAnyGroup) }));

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (OpB02RuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "ECN 作業站群組操作失敗", detail: ex.Message); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Problem(statusCode: 409, title: "ECN 作業站群組資料重複", detail: "群組編號或全域作業站識別碼已存在，請重新查詢。"); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("OPB02 database operation failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: 503, title: "ECN 作業站群組服務暫時無法使用", detail: "無法存取 ECN 主檔、明細或真實查找來源，請確認結構與必要權限已部署。");
        }
    }
}
