using System.Security.Claims;
using MES.Api.Data;
using MES.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MES.Api.Controllers;

[ApiController]
[Route("api/equipment/vendors")]
public sealed class EquipmentVendorsController(PostgresEquipmentVendorRepository repository) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<EquipmentVendorDto>> List([FromQuery] string? keyword, [FromQuery] int? issueState) =>
        Execute(() => repository.List(keyword, issueState));

    [HttpGet("{vendorNo}")]
    public ActionResult<EquipmentVendorDto> Get(string vendorNo) => Execute(() => repository.Get(vendorNo));

    [HttpPost]
    public IActionResult Create([FromBody] EquipmentVendorWriteRequest request) => CreateResult(() => repository.Create(request, ActorUserNo));

    [HttpPut("{vendorNo}")]
    public ActionResult<EquipmentVendorDto> Update(string vendorNo, [FromBody] EquipmentVendorWriteRequest request) =>
        Execute(() => repository.Update(vendorNo, request, ActorUserNo));

    [HttpPost("{vendorNo}/approve")]
    public IActionResult Approve(string vendorNo, [FromQuery] long dataStamp) =>
        ExecuteAction(() => repository.Approve(vendorNo, dataStamp, ActorUserNo));

    [HttpDelete("{vendorNo}")]
    public IActionResult Delete(string vendorNo, [FromQuery] int issueState, [FromQuery] long dataStamp) =>
        ExecuteAction(() => repository.Delete(vendorNo, issueState, dataStamp));

    [HttpGet("{vendorNo}/contactors")]
    public ActionResult<IReadOnlyList<EquipmentVendorContactorDto>> Contactors(string vendorNo) =>
        Execute(() => repository.Contactors(vendorNo));

    [HttpPost("{vendorNo}/contactors")]
    public IActionResult CreateContactor(string vendorNo, [FromBody] EquipmentVendorContactorWriteRequest request) =>
        CreateResult(() => repository.CreateContactor(vendorNo, request, ActorUserNo));

    [HttpPut("{vendorNo}/contactors/{contactorName}")]
    public ActionResult<EquipmentVendorContactorDto> UpdateContactor(string vendorNo, string contactorName, [FromBody] EquipmentVendorContactorWriteRequest request) =>
        Execute(() => repository.UpdateContactor(vendorNo, contactorName, request, ActorUserNo));

    [HttpDelete("{vendorNo}/contactors/{contactorName}")]
    public IActionResult DeleteContactor(string vendorNo, string contactorName, [FromQuery] long dataStamp) =>
        ExecuteAction(() => repository.DeleteContactor(vendorNo, contactorName, dataStamp));

    private string ActorUserNo => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "ymes";

    private ActionResult<T> Execute<T>(Func<T> action)
    {
        try { return Ok(action()); }
        catch (EquipmentRuleException ex) { return Problem(statusCode: ex.StatusCode, title: "設備供應商資料操作失敗", detail: ex.Message); }
    }

    private IActionResult CreateResult<T>(Func<T> action)
    {
        try { return Ok(action()); }
        catch (EquipmentRuleException ex) { return Problem(statusCode: ex.StatusCode, title: "設備供應商資料無法建立", detail: ex.Message); }
    }

    private IActionResult ExecuteAction(Action action)
    {
        try { action(); return NoContent(); }
        catch (EquipmentRuleException ex) { return Problem(statusCode: ex.StatusCode, title: "設備供應商資料操作失敗", detail: ex.Message); }
    }
}
