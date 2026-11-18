using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/s-class-basis")]
public sealed class Qc402Controller(NpgsqlDataSource dataSource, ILogger<Qc402Controller> logger) : ControllerBase
{
    private readonly Qc402Repository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? searchFieldName = null, [FromQuery] string? searchFieldValue = null) =>
        Execute(() => Ok(repository.List(searchFieldName, searchFieldValue)));

    [HttpGet("m-class-options")]
    public IActionResult MClassOptions() => Execute(() => Ok(repository.MClassOptions()));

    [HttpGet("{sClassNo}")]
    public IActionResult Get(string sClassNo) => Execute(() => Ok(repository.Get(sClassNo)));

    [HttpPost]
    public IActionResult Create([FromBody] Qc402SClassWriteRequest request) =>
        Write("Create", () => repository.Create(request, Actor));

    [HttpPut("{sClassNo}")]
    public IActionResult Update(string sClassNo, [FromQuery] int? issueState, [FromQuery] decimal? dataStamp,
        [FromBody] Qc402SClassWriteRequest request) =>
        Write("Update", () => repository.Update(sClassNo, request, issueState, RequireDataStamp(dataStamp), Actor));

    [HttpDelete("{sClassNo}")]
    public IActionResult Delete(string sClassNo, [FromQuery] int? issueState, [FromQuery] decimal? dataStamp) =>
        Write("Delete", () => repository.Delete(sClassNo, issueState, RequireDataStamp(dataStamp), Actor));

    [HttpPost("{sClassNo}/approve")]
    public IActionResult Approve(string sClassNo, [FromQuery] int? issueState, [FromQuery] decimal? dataStamp) =>
        Write("Approve", () => repository.Approve(sClassNo, issueState, RequireDataStamp(dataStamp), Actor));

    [HttpPost("import/preview")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public IActionResult Preview([FromForm] IFormFile? file) =>
        Execute(() => Ok(repository.Preview(file, HttpContext.RequestAborted)));

    [HttpPost("import")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public IActionResult Import([FromForm] IFormFile? file) => Execute(() =>
    {
        var results = repository.Import(file, Actor, HttpContext.RequestAborted);
        logger.LogInformation("QC402 Import completed by {Actor}; successful rows {SuccessCount}, failed rows {FailureCount}",
            Actor, results.Count(x => x.Result == "success"), results.Count(x => x.Result == "fail"));
        return Ok(results);
    });

    [HttpGet("import/template")]
    public IActionResult Template() => Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "QC402 正式範本尚未提供", detail: "YMES 尚未遷移 SCLASS BASIS 的原始 Excel 範本；請由公司端交付正式 SYS template storage 範本，不以 DEV CSV 取代。");

    private decimal? RequireDataStamp(decimal? dataStamp)
    {
        if (!Request.Query.ContainsKey("dataStamp"))
            throw new Qc402RuleException(StatusCodes.Status400BadRequest, "必須提供資料版本；空版本請明確傳入 dataStamp=。");
        return dataStamp;
    }

    private IActionResult Write(string actionName, Func<Qc402SClassDto> action) => Execute(() =>
    {
        var result = action();
        logger.LogInformation("QC402 {Action} completed for {Subject} by {Actor}; resulting state {State}, stamp {DataStamp}",
            actionName, result.SClassNo, Actor, result.IssueState, result.DataStamp);
        return Ok(result);
    });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc402RuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "QC402 操作失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogError("QC402 database operation failed for {Actor}; error {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "QC402 資料操作失敗",
                detail: "YMES 資料庫操作失敗，請確認次分類、主分類及引用資料表與權限已部署。");
        }
    }
}
