using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/sampling-plans")]
public sealed class Qc103Controller(NpgsqlDataSource dataSource, ILogger<Qc103Controller> logger) : ControllerBase
{
    private readonly Qc103Repository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    // Existing QC301 lookup controller retains GET collection/parameters routes.
    [HttpGet("{planNo}")]
    public IActionResult Get(string planNo) => Execute(() => Ok(repository.Get(planNo)));

    [HttpGet("options/plan-types")]
    public IActionResult PlanTypes() => Ok(Qc103Repository.PlanTypes);

    [HttpGet("options/parameters/{parameterNo}")]
    public IActionResult ParameterOptions(string parameterNo, [FromQuery] string? samplingType = null) =>
        Execute(() => Ok(repository.ParameterOptions(parameterNo, samplingType)));

    [HttpGet("options/functions")]
    public IActionResult FunctionOptions() => Problem(statusCode: 503, title: "抽樣函式尚未遷移",
        detail: "尚未交付原 udSamplingFunction 的 FUNSA 方法名單與實作；可維護函式名稱，但不提供猜測名單或執行任意函式。");

    [HttpPost]
    public IActionResult Create([FromBody] Qc103PlanWriteRequest request) =>
        Write("Create", () => repository.Create(request, Actor));

    [HttpPut("{planNo}")]
    public IActionResult Update(string planNo, [FromQuery] int? issueState, [FromQuery] bool? dataStampIsNull,
        [FromQuery] decimal? dataStamp, [FromBody] Qc103PlanWriteRequest request) =>
        Write("Update", () => repository.Update(planNo, request, issueState, ExpectedStamp(dataStampIsNull, dataStamp), Actor));

    [HttpDelete("{planNo}")]
    public IActionResult Delete(string planNo, [FromQuery] int? issueState, [FromQuery] bool? dataStampIsNull,
        [FromQuery] decimal? dataStamp) =>
        Write("Delete", () => repository.Delete(planNo, issueState, ExpectedStamp(dataStampIsNull, dataStamp), Actor));

    [HttpPost("{planNo}/approve")]
    public IActionResult Approve(string planNo, [FromQuery] int? issueState, [FromQuery] bool? dataStampIsNull,
        [FromQuery] decimal? dataStamp) =>
        Write("Approve", () => repository.Approve(planNo, issueState, ExpectedStamp(dataStampIsNull, dataStamp), Actor));

    [HttpPost("{planNo}/simulate")]
    public IActionResult Simulate(string planNo) => Execute(() =>
    {
        repository.Get(planNo);
        throw new Qc103RuleException(503, "正式抽樣計算 Core 尚未遷移；本次不以 DEV 示範或推算公式產生檢驗結果。");
    });

    private static decimal? ExpectedStamp(bool? isNull, decimal? stamp)
    {
        if (!isNull.HasValue) throw new Qc103RuleException(400, "必須提供 dataStampIsNull 與對應的版本條件。");
        if (isNull.Value)
        {
            if (stamp.HasValue) throw new Qc103RuleException(400, "dataStampIsNull=true 時不可同時提供數字 dataStamp。");
            return null;
        }
        if (!stamp.HasValue || decimal.Truncate(stamp.Value) != stamp.Value || stamp.Value < -99999999999m || stamp.Value > 99999999999m)
            throw new Qc103RuleException(400, "dataStampIsNull=false 時必須提供 11 位數範圍內的整數 DataStamp。");
        return stamp;
    }

    private IActionResult Write(string name, Func<Qc103PlanDto> action) => Execute(() =>
    {
        var result = action();
        logger.LogInformation("QC103 {Action} completed for {PlanNo} by {Actor}; state {State}, stamp {DataStamp}",
            name, result.PlanNo, Actor, result.IssueState, result.DataStamp);
        return Ok(result);
    });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc103RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "抽樣計劃操作失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogError("QC103 database operation failed for {Actor}; error {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: 503, title: "抽樣計劃資料服務暫時無法使用", detail: "YMES 抽樣計劃資料庫操作失敗，請確認主檔、參數與相關查找表及權限已部署。");
        }
    }
}
