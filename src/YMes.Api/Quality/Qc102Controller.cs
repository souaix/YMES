using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/sampling-1916")]
public sealed class Qc102Controller(NpgsqlDataSource dataSource, ILogger<Qc102Controller> logger) : ControllerBase
{
    private readonly Qc102Repository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    // The legacy page loads all rows once, then switches 0/1 locally.
    [HttpGet]
    public IActionResult List() => Execute(() => Ok(repository.List()));

    [HttpPost]
    public IActionResult Create([FromBody] Qc102SamplingCreateRequest request) =>
        ExecuteWrite("Create", () => repository.Create(request, Actor));

    [HttpPut("{samplingType}/{minNum}/{maxNum}/{sLevel}")]
    public IActionResult Update(string samplingType, decimal minNum, decimal maxNum, string sLevel,
        [FromBody] Qc102SamplingValuesRequest request,
        [FromQuery] bool? dataStampIsNull, [FromQuery] decimal? dataStamp) =>
        ExecuteWrite("Update", () => repository.Update(samplingType, minNum, maxNum, sLevel,
            ExpectedStamp(dataStampIsNull, dataStamp), request, Actor));

    [HttpDelete("{samplingType}/{minNum}/{maxNum}/{sLevel}")]
    public IActionResult Delete(string samplingType, decimal minNum, decimal maxNum, string sLevel,
        [FromQuery] bool? dataStampIsNull, [FromQuery] decimal? dataStamp) =>
        ExecuteWrite("Delete", () => repository.Delete(samplingType, minNum, maxNum, sLevel,
            ExpectedStamp(dataStampIsNull, dataStamp), Actor));

    private static decimal? ExpectedStamp(bool? isNull, decimal? stamp)
    {
        if (!isNull.HasValue) throw new Qc102RuleException(400, "必須提供 dataStampIsNull 與對應的版本條件。");
        if (isNull.Value)
        {
            if (stamp.HasValue) throw new Qc102RuleException(400, "dataStampIsNull=true 時不可同時提供數字 dataStamp。");
            return null;
        }
        if (!stamp.HasValue) throw new Qc102RuleException(400, "dataStampIsNull=false 時必須提供 dataStamp。");
        if (decimal.Truncate(stamp.Value) != stamp.Value || stamp.Value < -99999999999m || stamp.Value > 99999999999m)
            throw new Qc102RuleException(400, "DataStamp 必須是 11 位數範圍內的整數。");
        return stamp;
    }

    private IActionResult ExecuteWrite(string actionName, Func<Qc102SamplingDto> action) => Execute(() =>
    {
        var result = action();
        logger.LogInformation("QC102 {Action} completed for {SamplingType}/{MinNum}/{MaxNum}/{SLevel} by {Actor}; stamp {DataStamp}",
            actionName, result.SamplingType, result.MinNum, result.MaxNum, result.SLevel, Actor, result.DataStamp);
        return Ok(result);
    });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc102RuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "QC102 抽樣計劃操作失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogError("QC102 PostgreSQL operation failed for {Actor}; error {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "QC102 資料服務暫時無法使用", detail: "YMES 抽樣計劃資料庫操作失敗，請確認 TBLQCSAMPLING1916 已部署。");
        }
    }
}
