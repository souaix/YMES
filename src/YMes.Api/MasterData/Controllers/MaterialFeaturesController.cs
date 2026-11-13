using MES.Api.Data;
using MES.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MES.Api.Controllers;

[ApiController]
[Route("api/materials")]
public sealed class MaterialFeaturesController(PostgresMasterDataRepository repository) : ControllerBase
{
    private string ActorUserNo => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "ymes";

    [HttpGet("lookups/options")]
    public ActionResult<MaterialOptionsDto> Options() => Ok(new MaterialOptionsDto(
        repository.GetMaterialTypes(), repository.GetMaterialUnits(), repository.GetMaterialUnitTypes(),
        [new("0","EQP"),new("1","OP"),new("2","WIP INV"),new("3","MO")], [], []));

    [HttpGet("{no}/properties")]
    public IActionResult Properties(string no) => Execute(() => Ok(repository.GetMaterialProperties(no)));

    [HttpPost("{no}/properties")]
    public IActionResult SaveProperty(string no, [FromBody] MaterialPropertyWriteRequest request) => Execute(() => Ok(repository.SaveMaterialProperty(no, request, ActorUserNo)));

    [HttpDelete("{no}/properties/{propertyNo}")]
    public IActionResult DeleteProperty(string no, string propertyNo, [FromQuery] int dataStamp) => ExecuteAction(() => repository.DeleteMaterialProperty(no, propertyNo, dataStamp, ActorUserNo));

    [HttpPost("{no}/properties/{propertyNo}/approve")]
    public IActionResult ApproveProperty(string no, string propertyNo, [FromBody] MasterDataCommandRequest request) => Execute(() => Ok(repository.ApproveMaterialProperty(no, propertyNo, request.DataStamp, ActorUserNo)));

    [HttpGet("{no}/qc-items")]
    public IActionResult QCItems(string no) => Execute(() => Ok(repository.GetMaterialQCItems(no)));

    [HttpPut("{no}/qc-items")]
    public IActionResult SaveQCItems(string no, [FromBody] MaterialQCItemsWriteRequest request) => ExecuteAction(() => repository.SaveMaterialQCItems(no, request, ActorUserNo));

    [HttpGet("{no}/vendors")]
    public IActionResult Vendors(string no) => Execute(() => Ok(repository.GetMaterialVendors(no)));

    [HttpPost("{no}/vendors")]
    public IActionResult AddVendor(string no, [FromBody] MaterialVendorWriteRequest request) => Execute(() => Ok(repository.SaveMaterialVendor(no, request, ActorUserNo, false)));

    [HttpPut("{no}/vendors/{vendorNo}")]
    public IActionResult UpdateVendor(string no, string vendorNo, [FromBody] MaterialVendorWriteRequest request) => Execute(() =>
        Ok(repository.SaveMaterialVendor(no, request with { VendorNo = vendorNo }, ActorUserNo, true)));

    [HttpDelete("{no}/vendors/{vendorNo}")]
    public IActionResult DeleteVendor(string no, string vendorNo, [FromQuery] int dataStamp) => ExecuteAction(() => repository.DeleteMaterialVendor(no, vendorNo, dataStamp, ActorUserNo));

    [HttpGet("{no}/gum-hour")]
    public IActionResult GumHour(string no) => Execute(() => Ok(repository.GetMaterialGumHour(no)));

    [HttpPut("{no}/gum-hour")]
    public IActionResult SaveGumHour(string no, [FromBody] MaterialGumHourWriteRequest request) => Execute(() => Ok(repository.SaveMaterialGumHour(no, request, ActorUserNo)));

    [HttpPost("import")]
    public IActionResult Import([FromBody] IReadOnlyList<MaterialImportRowRequest> rows)
    {
        if (rows.Count > 1000) return BadRequest(new ProblemDetails { Status = 400, Title = "匯入失敗", Detail = "單次最多匯入 1000 筆。" });
        var results = new List<MaterialImportRowResult>(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            try
            {
                repository.CreateMaterial(new MaterialWriteRequest
                {
                    MaterialNo = row.MaterialNo, MaterialName = row.MaterialName, MaterialType = row.MaterialType,
                    UnitNo = row.UnitNo, UnitType = string.IsNullOrWhiteSpace(row.UnitType) ? repository.ResolveMaterialUnitType(row.UnitNo) : row.UnitType, MaterialSpec = row.MaterialSpec,
                    Description = row.Description, PutInPlace = row.PutInPlace, CountWay = row.CountWay,
                    CheckLotNo = row.CheckLotNo,
                }, ActorUserNo);
                results.Add(new(i + 1, row.MaterialNo, true, "匯入成功。"));
            }
            catch (MasterDataRuleException ex) { results.Add(new(i + 1, row.MaterialNo, false, ex.Message)); }
        }
        return Ok(results);
    }

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (MasterDataRuleException ex) { return Problem(statusCode: ex.StatusCode, title: "物料子資料操作失敗", detail: ex.Message); }
    }

    private IActionResult ExecuteAction(Action action)
    {
        try { action(); return NoContent(); }
        catch (MasterDataRuleException ex) { return Problem(statusCode: ex.StatusCode, title: "物料子資料操作失敗", detail: ex.Message); }
    }
}
