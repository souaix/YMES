using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Ems;

[ApiController]
[Authorize]
[Route("api/ems/equipment-capability-bases")]
public sealed class EquipmentCapabilityBasisController(
    EquipmentCapabilityBasisRepository repository,
    ILogger<EquipmentCapabilityBasisController> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult Search(
        [FromQuery] int issueState = 0,
        [FromQuery] string? capItem = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            return Ok(repository.Search(new EquipmentCapabilityBasisFilter(issueState, capItem, page, pageSize)));
        }
        catch (EquipmentCapabilityBasisRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "設備能力項目查詢條件無效", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("EMS equipment capability basis query failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: 503, title: "設備能力項目暫時無法查詢", detail: "目前無法讀取設備能力項目，請稍後重試。");
        }
    }

    [HttpPost]
    public IActionResult Create([FromBody] EquipmentCapabilityBasisCreateRequest request)
    {
        try { return Ok(repository.Create(request, Actor)); }
        catch (EquipmentCapabilityBasisRuleException ex) { return Problem(statusCode: ex.StatusCode, title: "設備能力項目無法新增", detail: ex.Message); }
        catch (NpgsqlException ex) { return DatabaseProblem(ex, "新增", Actor); }
    }

    [HttpPut("item")]
    public IActionResult Update([FromQuery] string capItem, [FromBody] EquipmentCapabilityBasisUpdateRequest request)
    {
        try { return Ok(repository.Update(capItem, request, Actor)); }
        catch (EquipmentCapabilityBasisRuleException ex) { return Problem(statusCode: ex.StatusCode, title: "設備能力項目無法修改", detail: ex.Message); }
        catch (NpgsqlException ex) { return DatabaseProblem(ex, "修改", Actor); }
    }

    [HttpPost("batch-delete")]
    public IActionResult Delete([FromBody] EquipmentCapabilityBasisDeleteRequest request)
    {
        try { return Ok(repository.Delete(request, Actor)); }
        catch (EquipmentCapabilityBasisRuleException ex) { return Problem(statusCode: ex.StatusCode, title: "設備能力項目無法刪除", detail: ex.Message); }
        catch (NpgsqlException ex) { return DatabaseProblem(ex, "刪除", Actor); }
    }

    [HttpPost("approve")]
    public IActionResult Approve([FromQuery] string capItem, [FromQuery] long dataStamp)
    {
        _ = capItem;
        _ = dataStamp;
        return Problem(statusCode: 409, title: "設備能力項目簽核尚未備妥",
            detail: "YMES 尚未提供可核對的 EMS_EQP_CAPBASIS 簽核流程設定；目前不會變更資料，請先完成公司端流程設定交接。");
    }

    private IActionResult DatabaseProblem(NpgsqlException ex, string operation, string actor)
    {
        logger.LogWarning("EMS equipment capability basis {Operation} failed ({ErrorType}) for {Actor}", operation, ex.GetType().Name, actor);
        return Problem(statusCode: 503, title: $"設備能力項目{operation}失敗", detail: "目前無法完成資料庫操作，請重新查詢確認後再重試。");
    }
}
