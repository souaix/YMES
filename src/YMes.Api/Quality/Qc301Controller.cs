using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Route("api/qc/chart-bases")]
public sealed class Qc301Controller(NpgsqlDataSource dataSource, ILogger<Qc301Controller> logger) : ControllerBase
{
    private readonly Qc301Repository _repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet("qc-items")]
    public IActionResult QcItems() => Execute(() => Ok(new { items = _repository.QcItems() }));

    [HttpGet]
    public IActionResult List([FromQuery] string qcItemNo, [FromQuery] int? issueState) =>
        Execute(() => Ok(new { items = _repository.List(qcItemNo, issueState) }));

    [HttpGet("{factorSerial}/detail")]
    public IActionResult Details(string factorSerial) => Execute(() => Ok(new { items = _repository.Details(factorSerial) }));

    [HttpGet("{factorSerial}/rules")]
    public IActionResult Rules(string factorSerial) => Execute(() => Ok(new { items = _repository.Rules(factorSerial) }));

    [HttpPost]
    public IActionResult Create([FromBody] Qc301ChartWriteRequest request) => Mutate("create", request.QcItemNo,
        () => Ok(_repository.Create(request, Actor)));

    [HttpPut("{factorSerial}")]
    public IActionResult Update(string factorSerial, [FromBody] Qc301ChartUpdateRequest request) => Mutate("update", factorSerial,
        () => Ok(_repository.Update(factorSerial, request, Actor)));

    [HttpDelete("{factorSerial}")]
    public IActionResult Delete(string factorSerial, [FromBody] Qc301StateRequest request) => Mutate("delete", factorSerial,
        () => Ok(_repository.Delete(factorSerial, request, Actor)));

    [HttpPost("{factorSerial}/approve")]
    public IActionResult Approve(string factorSerial, [FromBody] Qc301StateRequest request) => Mutate("approve", factorSerial,
        () => Ok(_repository.Approve(factorSerial, request, Actor)));

    [HttpPost("{factorSerial}/disapprove")]
    public IActionResult Disapprove(string factorSerial, [FromBody] Qc301StateRequest request) => Mutate("disapprove", factorSerial,
        () => Ok(_repository.Disapprove(factorSerial, request, Actor)));

    [HttpPut("{factorSerial}/rules")]
    public IActionResult ReplaceRules(string factorSerial, [FromBody] Qc301RulesRequest request) => Mutate("rules", factorSerial,
        () => Ok(_repository.ReplaceRules(factorSerial, request, Actor)));

    [HttpPost("copy")]
    public IActionResult Copy([FromBody] Qc301CopyRequest request) => Mutate("copy", request.FromQcItemNo,
        () => Ok(_repository.Copy(request, Actor)));

    private IActionResult Mutate(string actionName, string subject, Func<IActionResult> action)
    {
        try
        {
            var result = action();
            logger.LogInformation("QC301 {Action} completed for {Subject} by {Actor}", actionName, subject, Actor);
            return result;
        }
        catch (Qc301RuleException ex)
        {
            logger.LogWarning("QC301 {Action} rejected for {Subject} by {Actor}: {Reason}", actionName, subject, Actor, ex.Message);
            return Problem(statusCode: ex.StatusCode, title: "QC301 管制圖資料操作失敗", detail: ex.Message);
        }
        catch (PostgresException)
        {
            logger.LogError("QC301 {Action} failed in PostgreSQL for {Subject} by {Actor}", actionName, subject, Actor);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "QC301 資料操作失敗", detail: "資料庫操作失敗，請確認 YMES schema 與相依資料已部署。");
        }
    }

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc301RuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "QC301 管制圖資料讀取失敗", detail: ex.Message);
        }
        catch (PostgresException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "QC301 資料讀取失敗", detail: "資料庫操作失敗，請確認 YMES schema 與相依資料已部署。");
        }
    }
}

[ApiController]
[Route("api/qc/sampling-plans")]
public sealed class Qc301SamplingPlanLookupController(NpgsqlDataSource dataSource) : ControllerBase
{
    private readonly Qc301Repository _repository = new(dataSource);

    [HttpGet]
    public IActionResult List([FromQuery] int? issueState, [FromQuery] int? componentCollect) => Execute(() => Ok(new { items = _repository.SamplingPlans(issueState, componentCollect) }));

    [HttpGet("{planNo}/parameters")]
    public IActionResult Parameters(string planNo, [FromQuery] int? issueState) => Execute(() => Ok(new { items = _repository.SamplingPlanParameters(planNo, issueState) }));

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc301RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "抽樣計畫查詢失敗", detail: ex.Message); }
        catch (PostgresException) { return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "抽樣計畫查詢失敗", detail: "資料庫操作失敗，請確認 YMES 抽樣計畫相依表已部署。"); }
    }
}
