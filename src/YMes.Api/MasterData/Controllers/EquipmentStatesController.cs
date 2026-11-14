using MES.Api.Data;
using MES.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MES.Api.Controllers;

[ApiController]
[Route("api/equipment/states")]
public sealed class EquipmentStatesController(PostgresEquipmentStateRepository repository) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<EquipmentStateDto>> List() => Execute(repository.List);

    [HttpGet("options")]
    public ActionResult<EquipmentStateOptionsDto> Options() => Execute(repository.Options);

    [HttpPost]
    public ActionResult<EquipmentStateDto> Create([FromBody] EquipmentStateWriteRequest request) => Execute(() => repository.Create(request));

    [HttpPut("{equipmentState:int}")]
    public ActionResult<EquipmentStateDto> Update(int equipmentState, [FromBody] EquipmentStateWriteRequest request) =>
        Execute(() => repository.Update(equipmentState, request));

    [HttpDelete("{equipmentState:int}")]
    public IActionResult Delete(int equipmentState)
    {
        try
        {
            repository.Delete(equipmentState);
            return NoContent();
        }
        catch (EquipmentRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "設備狀態資料操作失敗", detail: ex.Message);
        }
    }

    private ActionResult<T> Execute<T>(Func<T> action)
    {
        try { return Ok(action()); }
        catch (EquipmentRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "設備狀態資料操作失敗", detail: ex.Message);
        }
    }

    private IActionResult Execute(Action action)
    {
        try { action(); return NoContent(); }
        catch (EquipmentRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "設備狀態資料操作失敗", detail: ex.Message);
        }
    }
}
