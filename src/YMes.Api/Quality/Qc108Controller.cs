using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/rule-bases")]
public sealed class Qc108Controller(NpgsqlDataSource dataSource, ILogger<Qc108Controller> logger) : ControllerBase
{
    private readonly Qc108Repository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List() => Execute(() => Ok(repository.List()));

    [HttpGet("options/rule-options")]
    public IActionResult RuleOptions() => Ok(Qc108Repository.RuleOptions);

    [HttpGet("options/functions")]
    public IActionResult Functions() => Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "QC108 函式目錄尚未交付", detail: "尚未取得 udQCRule.clsQCRule 的有效 FUNQR 函式目錄；System 法則仍可維護，User Define 不接受 DEMO 名稱或任意函式。");

    [HttpPost]
    public IActionResult Create([FromBody] Qc108RuleWriteRequest request) => Write("Create", () => repository.Create(request, Actor));

    [HttpPut("{ruleNo}")]
    public IActionResult Update(string ruleNo, [FromQuery] int? issueState, [FromQuery] decimal? dataStamp,
        [FromBody] Qc108RuleWriteRequest request) => Write("Update", () =>
            repository.Update(ruleNo, request, Expected(issueState, dataStamp), Actor));

    [HttpDelete("{ruleNo}")]
    public IActionResult Delete(string ruleNo, [FromQuery] int? issueState, [FromQuery] decimal? dataStamp) =>
        Write("Delete", () => repository.Delete(ruleNo, Expected(issueState, dataStamp), Actor));

    [HttpPost("{ruleNo}/approve")]
    public IActionResult Approve(string ruleNo, [FromQuery] int? issueState, [FromQuery] decimal? dataStamp) =>
        Write("Approve", () => repository.Approve(ruleNo, Expected(issueState, dataStamp), Actor));

    private Qc108ExpectedVersion Expected(int? state, decimal? stamp)
    {
        if (!state.HasValue || state.Value is not (-1 or 0 or 1 or 2))
            throw new Qc108RuleException(400, "IssueState 必須提供，且僅允許 -1、0、1、2。");
        if (!Request.Query.ContainsKey("dataStamp"))
            throw new Qc108RuleException(400, "必須提供 dataStamp；來源版本為 NULL 時請使用 dataStamp=。");
        if (stamp.HasValue && (stamp.Value != decimal.Truncate(stamp.Value) || stamp.Value < -99999999999m || stamp.Value > 99999999999m))
            throw new Qc108RuleException(400, "DataStamp 必須是 11 位數範圍內的整數或明確空值。");
        return new(state.Value, stamp);
    }

    private IActionResult Write(string actionName, Func<Qc108RuleDto> action) => Execute(() =>
    {
        var result = action();
        logger.LogInformation("QC108 {Action} completed for {RuleSerial}/{RuleNo} by {Actor}; state {State}, stamp {Stamp}",
            actionName, result.RuleSerial, result.RuleNo, Actor, result.IssueState, result.DataStamp);
        return Ok(result);
    });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc108RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "QC108 計量品管法則操作失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogError("QC108 database operation failed for {Actor}; error {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "QC108 資料操作失敗",
                detail: "YMES 計量品管法則資料庫操作失敗，請確認 TBLQCRULEBASIS 與必要資源已部署。");
        }
    }
}
