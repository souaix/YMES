using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/m-class-basis")]
public sealed class Qc401Controller(NpgsqlDataSource dataSource, ILogger<Qc401Controller> logger) : ControllerBase
{
    private readonly Qc401Repository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? searchFieldName = null, [FromQuery] string? searchFieldValue = null) =>
        Execute(() => Ok(repository.List(searchFieldName, searchFieldValue)));

    [HttpGet("{mClassNo}")]
    public IActionResult Get(string mClassNo) => Execute(() => Ok(repository.Get(mClassNo)));

    [HttpPost]
    public IActionResult Create([FromBody] Qc401MClassWriteRequest request) => Write("Create", () => repository.Create(request, Actor));

    [HttpPut("{mClassNo}")]
    public IActionResult Update(string mClassNo, [FromQuery] int? issueState, [FromQuery] decimal? dataStamp,
        [FromBody] Qc401MClassWriteRequest request) => Write("Update", () => repository.Update(mClassNo, request, issueState, ExpectedStamp(dataStamp), Actor));

    [HttpDelete("{mClassNo}")]
    public IActionResult Delete(string mClassNo, [FromQuery] int? issueState, [FromQuery] decimal? dataStamp) =>
        Write("Delete", () => repository.Delete(mClassNo, issueState, ExpectedStamp(dataStamp), Actor));

    [HttpPost("{mClassNo}/approve")]
    public IActionResult Approve(string mClassNo, [FromQuery] int? issueState, [FromQuery] decimal? dataStamp) =>
        Write("Approve", () => repository.Approve(mClassNo, issueState, ExpectedStamp(dataStamp), Actor));

    [HttpPost("import/preview")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public IActionResult Preview([FromForm] IFormFile? file) => Execute(() => Ok(repository.Preview(file)));

    [HttpPost("import")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public IActionResult Import([FromForm] IFormFile? file) => Execute(() =>
    {
        var results = repository.Import(file, Actor, HttpContext.RequestAborted);
        logger.LogInformation("QC401 Import completed by {Actor}; successful rows {SuccessCount}, failed rows {FailureCount}",
            Actor, results.Count(x => x.Result == "success"), results.Count(x => x.Result == "fail"));
        return Ok(results);
    });

    [HttpGet("import/template")]
    public IActionResult Template() => Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "QC401 正式範本尚未提供", detail: "YMES 尚未遷移 MCLASS BASIS 的原始 Excel 範本；請由公司端交付正式 SYS template storage 範本，不以 DEV CSV 取代。");

    private decimal? ExpectedStamp(decimal? stamp)
    {
        if (!Request.Query.ContainsKey("dataStamp"))
            throw new Qc401RuleException(400, "必須提供 dataStamp 版本條件；原值為 NULL 時請明確傳入 dataStamp=。");
        if (stamp.HasValue && (decimal.Truncate(stamp.Value) != stamp.Value || stamp.Value < -99999999999m || stamp.Value > 99999999999m))
            throw new Qc401RuleException(400, "DataStamp 必須為 NULL 或 11 位數範圍內的整數。");
        return stamp;
    }

    private IActionResult Write(string actionName, Func<Qc401MClassDto> action) => Execute(() =>
    {
        var result = action();
        logger.LogInformation("QC401 {Action} completed for {Subject} by {Actor}; resulting state {State}, stamp {DataStamp}",
            actionName, result.MClassNo, Actor, result.IssueState, result.DataStamp);
        return Ok(result);
    });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc401RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "QC401 操作失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogError("QC401 database operation failed for {Actor}; error {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "QC401 資料操作失敗",
                detail: "YMES 資料庫操作失敗，請確認主分類與引用資料表、權限已部署。");
        }
    }
}
