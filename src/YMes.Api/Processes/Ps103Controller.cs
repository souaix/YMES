using System.Security.Claims;
using MES.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Processes;

[ApiController]
[Authorize]
[Route("api/process-types")]
public sealed class Ps103Controller(NpgsqlDataSource dataSource, ILogger<Ps103Controller> logger) : ControllerBase
{
    private readonly Ps103Repository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] int? issueState = null, [FromQuery] string? processType = null) =>
        Execute(() => Ok(repository.List(issueState, processType)));

    [HttpGet("{processType}")]
    public IActionResult Get(string processType) => Execute(() => Ok(repository.Get(processType)));

    // PS103 owns this shared lookup; PS104 must reuse it, not register a duplicate.
    [HttpGet("/api/process-basis/lookups/process-types")]
    public IActionResult ApprovedTypes([FromQuery] int? issueState = 2) => Execute(() =>
    {
        if (issueState != 2) throw new Ps103RuleException(400, "流程類別查找只提供已核准（issueState=2）的資料。");
        return Ok(repository.List(2, null));
    });

    [HttpPost]
    public IActionResult Create([FromBody] Ps103TypeWriteRequest request) => Write("Create", () => repository.Create(request, Actor));

    [HttpPut("{processType}")]
    public IActionResult Update(string processType, [FromBody] Ps103TypeUpdateRequest request) => Write("Update", () => repository.Update(processType, request, Actor));

    [HttpDelete("{processType}")]
    public IActionResult Delete(string processType, [FromBody] Ps103DeleteRequest request) => Write("Delete", () => repository.Delete(processType, request, Actor));

    [HttpPost("{processType}/approve")]
    public IActionResult Approve(string processType, [FromBody] Ps103StampRequest request) => Write("Approve", () => repository.Approve(processType, request, Actor));

    private IActionResult Write(string operation, Func<Ps103TypeDto> action) => Execute(() =>
    {
        var result = action();
        logger.LogInformation("PS103 {Operation} for {ProcessType} by {Actor}; resulting state {State}, stamp {DataStamp}",
            operation, result.ProcessType, Actor, result.IssueState, result.DataStamp);
        return Ok(result);
    });
    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (MasterDataRuleException ex) { return Problem(statusCode: ex.StatusCode, title: "流程類別操作失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogError("PS103 database operation failed for {Actor}; error {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: 503, title: "流程類別資料服務暫時無法使用", detail: "YMES 流程類別資料庫操作失敗，請確認主檔、相關流程／屬性表與應用權限已部署。");
        }
    }
}
