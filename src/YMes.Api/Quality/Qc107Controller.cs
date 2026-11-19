using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/attribute-rule-bases")]
public sealed class Qc107Controller(NpgsqlDataSource dataSource, IConfiguration configuration, ILogger<Qc107Controller> logger) : ControllerBase
{
    private readonly Qc107Repository repository = new(dataSource, configuration);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? searchFieldName = null, [FromQuery] string? searchFieldValue = null) =>
        Execute(() => Ok(repository.List(searchFieldName, searchFieldValue)));

    [HttpGet("{ruleNo}")]
    public IActionResult Get(string ruleNo) => Execute(() => Ok(repository.Get(ruleNo)));

    [HttpGet("{ruleNo}/details")]
    public IActionResult Details(string ruleNo) => Execute(() => Ok(repository.Get(ruleNo).Details));

    [HttpGet("options/actions")]
    public IActionResult Actions() => Execute(() => Ok(repository.ValueList("Actions")));

    [HttpGet("options/reason-types")]
    public IActionResult ReasonTypes() => Execute(() => Ok(repository.ValueList("ReasonTypes")));

    [HttpGet("options/aql")]
    public IActionResult Aql() => Execute(() => Ok(repository.AqlOptions()));

    [HttpPost]
    public IActionResult Create([FromBody] Qc107WriteRequest request) => Write("Create", () => repository.Create(request, Actor));

    [HttpPut("{ruleNo}")]
    public IActionResult Update(string ruleNo, [FromQuery] int? issueState, [FromQuery] string? version,
        [FromBody] Qc107WriteRequest request) =>
        Write("Update", () => repository.Update(ruleNo, ExpectedState(issueState), ExpectedVersion(version), request, Actor));

    [HttpDelete("{ruleNo}")]
    public IActionResult Delete(string ruleNo, [FromQuery] int? issueState, [FromQuery] string? version) =>
        Write("Delete", () => repository.Delete(ruleNo, ExpectedState(issueState), ExpectedVersion(version), Actor));

    [HttpPost("{ruleNo}/approve")]
    public IActionResult Approve(string ruleNo, [FromQuery] int? issueState, [FromQuery] string? version) =>
        Write("Approve", () => repository.Approve(ruleNo, ExpectedState(issueState), ExpectedVersion(version), Actor));

    private int? ExpectedState(int? state)
    {
        if (!Request.Query.ContainsKey("issueState") || (state.HasValue && state.Value is not (-1 or 0 or 1 or 2)))
            throw new Qc107RuleException(400, "必須明確提供 issueState；NULL 請傳 issueState=，數值只允許 -1、0、1、2。");
        return state;
    }

    private static string ExpectedVersion(string? version)
    {
        if (version is null || version.Length != 64 || !version.All(Uri.IsHexDigit))
            throw new Qc107RuleException(400, "必須提供查詢取得的完整 version，不可省略版本條件。");
        return version;
    }

    private IActionResult Write(string actionName, Func<Qc107SaveResult> action) => Execute(() =>
    {
        var result = action();
        logger.LogInformation("QC107 {Action} for {RuleNo} by {Actor}; resulting state {State}, detail count {DetailCount}",
            actionName, result.Basis.RuleNo, Actor, result.Basis.IssueState, result.Details.Count);
        return Ok(result);
    });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc107RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "QC107 計數判別法則操作失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogError("QC107 database operation failed for {Actor}; error {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: 503, title: "QC107 資料服務暫時無法使用",
                detail: "YMES 計數判別法則資料庫操作失敗，請確認主檔、明細及相關資料表和權限已部署。");
        }
    }
}
