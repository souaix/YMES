using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/sampling-tables")]
public sealed class Qc101Controller(NpgsqlDataSource dataSource, ILogger<Qc101Controller> logger) : ControllerBase
{
    private readonly Qc101Repository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? searchFieldName = null, [FromQuery] string? searchFieldValue = null) =>
        Execute(() => Ok(repository.List(searchFieldName, searchFieldValue)));

    [HttpPost]
    public IActionResult Create([FromBody] Qc101SamplingTableCreateRequest request) =>
        ExecuteWrite("Create", () => repository.Create(request, Actor));

    [HttpPost("copy")]
    public IActionResult Copy([FromBody] Qc101CopyRequest request) => Execute(() =>
    {
        var result = repository.Copy(request, Actor);
        logger.LogInformation("QC101 CopyTable {Source} to {Target} by {Actor}: inserted {Inserted}, skipped {Skipped}, partial failure {HasError}",
            request.SourceSamplingTable, request.TargetSamplingTable, Actor, result.InsertedCount, result.SkippedCount, result.Error is not null);
        return Ok(result);
    });

    [HttpPut("{samplingTable}/{minNum}/{maxNum}")]
    public IActionResult Update(string samplingTable, decimal minNum, decimal maxNum,
        [FromBody] Qc101SamplingTableValuesRequest request,
        [FromQuery] bool? dataStampIsNull, [FromQuery] decimal? dataStamp) =>
        ExecuteWrite("Update", () => repository.Update(samplingTable, minNum, maxNum,
            ExpectedStamp(dataStampIsNull, dataStamp), request, Actor));

    [HttpDelete("{samplingTable}/{minNum}/{maxNum}")]
    public IActionResult Delete(string samplingTable, decimal minNum, decimal maxNum,
        [FromQuery] bool? dataStampIsNull, [FromQuery] decimal? dataStamp) =>
        ExecuteWrite("Delete", () => repository.Delete(samplingTable, minNum, maxNum,
            ExpectedStamp(dataStampIsNull, dataStamp), Actor));

    private static decimal? ExpectedStamp(bool? isNull, decimal? stamp)
    {
        if (!isNull.HasValue) throw new Qc101RuleException(400, "必須提供 dataStampIsNull 與對應的版本條件。");
        if (isNull.Value)
        {
            if (stamp.HasValue) throw new Qc101RuleException(400, "dataStampIsNull=true 時不可同時提供數字 dataStamp。");
            return null;
        }
        if (!stamp.HasValue) throw new Qc101RuleException(400, "dataStampIsNull=false 時必須提供 dataStamp。");
        if (decimal.Truncate(stamp.Value) != stamp.Value || stamp.Value < -99999999999m || stamp.Value > 99999999999m)
            throw new Qc101RuleException(400, "DataStamp 必須是 11 位數範圍內的整數。");
        return stamp;
    }

    private IActionResult ExecuteWrite(string actionName, Func<Qc101SamplingTableDto> action) => Execute(() =>
    {
        var result = action();
        logger.LogInformation("QC101 {Action} completed for {SamplingTable}/{MinNum}/{MaxNum} by {Actor}; stamp {DataStamp}",
            actionName, result.SamplingTable, result.MinNum, result.MaxNum, Actor, result.DataStamp);
        return Ok(result);
    });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc101RuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "QC101 抽樣表格操作失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogError("QC101 PostgreSQL operation failed for {Actor}; error {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "QC101 資料服務暫時無法使用", detail: "YMES 抽樣表格資料庫操作失敗，請確認 TBLQCSAMPLINGTABLE 已部署。");
        }
    }
}
