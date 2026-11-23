using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Ems;

[ApiController]
[Authorize]
[Route("api/ems/equipment-accessory-state-logs")]
public sealed class EquipmentAccessoryStateLogsController(
    EquipmentAccessoryStateLogRepository repository,
    ILogger<EquipmentAccessoryStateLogsController> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? equipmentType,
        [FromQuery] string? equipmentNo,
        [FromQuery] string? userNo,
        [FromQuery] string? accessoryType,
        [FromQuery] string? accessoryNo,
        [FromQuery] string? startTimeFrom,
        [FromQuery] string? startTimeTo,
        [FromQuery] string? endTimeFrom,
        [FromQuery] string? endTimeTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await repository.SearchAsync(
                new EquipmentAccessoryStateLogFilter(
                    equipmentType, equipmentNo, userNo, accessoryType, accessoryNo,
                    startTimeFrom, startTimeTo, endTimeFrom, endTimeTo, page, pageSize),
                cancellationToken);
            return Ok(result);
        }
        catch (EquipmentAccessoryStateLogRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "設備配件歷程查詢條件無效", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("EMS equipment accessory history query failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: 503, title: "設備配件歷程暫時無法查詢",
                detail: "目前無法讀取設備配件歷程，請稍後重試。");
        }
    }
}
