using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Reports;

[ApiController]
[Authorize]
[Route("api/reports/equipment-daily-utilization")]
public sealed class EquipmentDailyUtilizationController(
    EquipmentDailyUtilizationRepository repository,
    ILogger<EquipmentDailyUtilizationController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] string? areaNos = null,
        [FromQuery] string? equipmentNos = null,
        [FromQuery] bool chamberManage = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await repository.GetAsync(startTime, endTime, areaNos, equipmentNos,
                chamberManage, cancellationToken));
        }
        catch (EquipmentDailyUtilizationRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "設備稼動查詢失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("RT410 query failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: 503, title: "設備稼動資料服務暫時無法使用",
                detail: "設備、狀態或 WIP 報表資料結構及應用程式讀取權限尚未就緒。");
        }
    }
}
