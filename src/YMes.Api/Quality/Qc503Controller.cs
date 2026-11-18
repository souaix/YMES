using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/inspection-drivers")]
public sealed class Qc503Controller(NpgsqlDataSource dataSource, ILogger<Qc503Controller> logger) : ControllerBase
{
    private readonly Qc503Repository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    // No state query preserves the all-state lookup shared by QC501/QC502.
    [HttpGet]
    public IActionResult List([FromQuery] int? issueState = null, [FromQuery] string? inspDriverNo = null) =>
        Execute(() => Ok(new { items = repository.List(issueState, inspDriverNo) }));

    [HttpGet("functions")]
    public IActionResult Functions() => Ok(new { items = Qc503Repository.AllowedFunctions });

    [HttpGet("{inspDriverNo}")]
    public IActionResult Get(string inspDriverNo) => Execute(() => Ok(repository.Get(inspDriverNo)));

    [HttpPost]
    public IActionResult Create([FromBody] Qc503DriverWriteRequest request) =>
        ExecuteWrite("Create", request.InspDriverNo, () => repository.Create(request, Actor));

    [HttpPut("{inspDriverNo}")]
    public IActionResult Update(string inspDriverNo, [FromBody] Qc503DriverUpdateRequest request) =>
        ExecuteWrite("Update", inspDriverNo, () => repository.Update(inspDriverNo, request, Actor));

    [HttpDelete("{inspDriverNo}")]
    public IActionResult Delete(string inspDriverNo, [FromQuery] int issueState, [FromQuery] decimal? dataStamp)
    {
        if (!Request.Query.ContainsKey(nameof(dataStamp)))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "QC503 請求缺少版本",
                detail: "刪除必須提供 dataStamp；來源版本為 NULL 時請使用 dataStamp=。");
        return ExecuteWrite("Delete", inspDriverNo, () => repository.Delete(inspDriverNo, issueState, dataStamp, Actor));
    }

    [HttpPost("{inspDriverNo}/approve")]
    public IActionResult Approve(string inspDriverNo, [FromBody] Qc503DriverStateRequest request) =>
        ExecuteWrite("Approve", inspDriverNo, () => repository.Approve(inspDriverNo, request, Actor));

    private IActionResult ExecuteWrite(string actionName, string subject, Func<Qc503DriverDto> action) =>
        Execute(() =>
        {
            var result = action();
            logger.LogInformation("QC503 {Action} completed for {Subject} by {Actor}; resulting state {State}, stamp {DataStamp}",
                actionName, result.InspDriverNo, Actor, result.IssueState, result.DataStamp);
            return Ok(result);
        });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc503RuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "QC503 操作失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogError("QC503 PostgreSQL operation failed for {Actor}; error {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "QC503 資料操作失敗", detail: "YMES 資料庫操作失敗，請確認檢測儀器共用資料表與必要資源已部署。");
        }
    }
}
