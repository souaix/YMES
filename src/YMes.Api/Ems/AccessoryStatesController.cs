using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Ems;

[ApiController]
[Authorize]
[Route("api/ems/accessory-states")]
public sealed class AccessoryStatesController(
    AccessoryStateRepository repository,
    ILogger<AccessoryStatesController> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List(
        [FromQuery] string? accessoryType,
        [FromQuery] string? accessoryNo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try { return Ok(repository.List(accessoryType, accessoryNo, page, pageSize)); }
        catch (AccessoryStateRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "配件狀態查詢條件無效", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("EMS accessory state query failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: 503, title: "配件狀態查詢暫時無法使用",
                detail: "目前無法讀取配件狀態資料，請稍後重試。");
        }
    }
}
