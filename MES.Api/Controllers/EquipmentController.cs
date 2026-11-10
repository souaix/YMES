using MES.Api.Data;
using MES.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MES.Api.Controllers;

[ApiController]
[Route("api/equipment")]
public sealed class EquipmentController(EquipmentRepository repository) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<EquipmentDto>> List([FromQuery] string? keyword, [FromQuery] int? issueState, [FromQuery] string? equipmentType) =>
        Ok(repository.List(keyword, issueState, equipmentType));

    [HttpGet("{equipmentNo}")]
    public ActionResult<EquipmentDto> Get(string equipmentNo) => Execute(() => repository.Get(equipmentNo));

    [HttpGet("{equipmentNo}/chambers")]
    public ActionResult<IReadOnlyList<EquipmentDto>> Chambers(string equipmentNo) => Execute(() => repository.Chambers(equipmentNo));

    [HttpPost("{equipmentNo}/chambers")]
    public ActionResult<EquipmentDto> CreateChamber(string equipmentNo, [FromBody] ChamberWriteRequest request) => Create(() => repository.CreateChamber(equipmentNo, request), x => $"/api/equipment/{Uri.EscapeDataString(equipmentNo)}/chambers/{Uri.EscapeDataString(x.EquipmentNo)}");

    [HttpPut("{equipmentNo}/chambers/{chamberNo}")]
    public ActionResult<EquipmentDto> UpdateChamber(string equipmentNo, string chamberNo, [FromBody] ChamberWriteRequest request) => Execute(() => repository.UpdateChamber(equipmentNo, chamberNo, request), "Chamber 無法更新");

    [HttpPost("{equipmentNo}/chambers/{chamberNo}/approve")]
    public ActionResult<EquipmentDto> ApproveChamber(string equipmentNo, string chamberNo, [FromBody] EquipmentCommandRequest request) => Execute(() => repository.ApproveChamber(equipmentNo, chamberNo, request.DataStamp), "Chamber 無法核准");

    [HttpDelete("{equipmentNo}/chambers/{chamberNo}")]
    public ActionResult<EquipmentDto> DeleteChamber(string equipmentNo, string chamberNo, [FromQuery] int issueState, [FromQuery] int dataStamp) => Execute(() => repository.DeleteChamber(equipmentNo, chamberNo, issueState, dataStamp), "Chamber 無法刪除或停用");

    [HttpPost]
    public ActionResult<EquipmentDto> Create([FromBody] EquipmentWriteRequest request)
    {
        try
        {
            var item = repository.Create(request);
            return CreatedAtAction(nameof(Get), new { equipmentNo = item.EquipmentNo }, item);
        }
        catch (EquipmentRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "設備資料無法建立", detail: ex.Message);
        }
    }

    [HttpPut("{equipmentNo}")]
    public ActionResult<EquipmentDto> Update(string equipmentNo, [FromBody] EquipmentWriteRequest request)
    {
        if (!string.Equals(equipmentNo, request.EquipmentNo, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new ProblemDetails { Title = "設備編號不一致", Detail = "網址與資料內容的 EquipmentNo 必須相同。", Status = 400 });
        return Execute(() => repository.Update(equipmentNo, request), "設備資料無法更新");
    }

    [HttpPost("{equipmentNo}/approve")]
    public ActionResult<EquipmentDto> Approve(string equipmentNo, [FromBody] EquipmentCommandRequest request) =>
        Execute(() => repository.Approve(equipmentNo, request.DataStamp), "設備無法核准");

    [HttpDelete("{equipmentNo}")]
    public ActionResult<EquipmentDto> Delete(string equipmentNo, [FromQuery] int issueState, [FromQuery] int dataStamp) =>
        Execute(() => repository.DeleteOrRetire(equipmentNo, issueState, dataStamp), "設備無法刪除或停用");

    private ActionResult<T> Execute<T>(Func<T> action, string title = "設備資料讀取失敗")
    {
        try
        {
            return Ok(action());
        }
        catch (EquipmentRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: title, detail: ex.Message);
        }
    }

    private ActionResult<T> Create<T>(Func<T> action, Func<T, string> location)
    {
        try
        {
            var item = action();
            return Created(location(item), item);
        }
        catch (EquipmentRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "Chamber 無法建立", detail: ex.Message);
        }
    }
}

public sealed record EquipmentCommandRequest(int DataStamp);
