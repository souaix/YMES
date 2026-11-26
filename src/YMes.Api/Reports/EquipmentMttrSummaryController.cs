using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Reports;

[ApiController]
[Authorize]
[Route("api/reports/equipment-mttr-summary")]
public sealed class EquipmentMttrSummaryController(
    EquipmentMttrSummaryRepository repository,
    ILogger<EquipmentMttrSummaryController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? areaNos = null,
        [FromQuery] string? equipmentTypes = null,
        [FromQuery] string? equipmentNos = null,
        [FromQuery] bool includeChambers = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await repository.GetAsync(startDate, endDate, areaNos, equipmentTypes,
                equipmentNos, includeChambers, cancellationToken));
        }
        catch (EquipmentMttrSummaryRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "設備 MTTR 查詢失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("RT412 query failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: 503, title: "設備 MTTR 資料服務暫時無法使用",
                detail: "資料庫結構或應用程式讀取權限尚未就緒。");
        }
    }
}
