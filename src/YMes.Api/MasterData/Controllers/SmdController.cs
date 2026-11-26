using System.Security.Claims;
using MES.Api.Data;
using MES.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MES.Api.Controllers;

[ApiController]
[Route("api/smd")]
public sealed class SmdController(
    PostgresSmdRepository repository,
    PostgresSmdAreaInventoryRepository areaInventoryRepository,
    PostgresSmdAreaBasisRepository areaBasisRepository,
    PostgresSmdFactoryBasisRepository factoryBasisRepository,
    PostgresSmdLocatorBasisRepository locatorBasisRepository,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("locator-basis")]
    public ActionResult<IReadOnlyList<SmdLocatorBasisDto>> LocatorBasis([FromQuery] string? inventoryNo) =>
        Execute(() => locatorBasisRepository.List(inventoryNo), "儲位基本資料讀取失敗");

    [HttpGet("locator-basis/options")]
    public ActionResult<SmdLocatorBasisOptionsDto> LocatorBasisOptions() =>
        Execute(locatorBasisRepository.Options, "已核准庫存選項讀取失敗");

    [HttpPost("locator-basis")]
    public ActionResult<SmdLocatorBasisDto> CreateLocatorBasis([FromBody] SmdLocatorBasisWriteRequest request) =>
        Execute(() => locatorBasisRepository.Create(request, ActorUserNo), "儲位基本資料無法建立");

    [HttpPut("locator-basis/item")]
    public ActionResult<SmdLocatorBasisDto> UpdateLocatorBasis(
        [FromQuery] string inventoryNo,
        [FromQuery] string locatorNo,
        [FromQuery] int issueState,
        [FromQuery] long dataStamp,
        [FromBody] SmdLocatorBasisWriteRequest request) =>
        Execute(() => locatorBasisRepository.Update(inventoryNo, locatorNo, issueState, dataStamp, request, ActorUserNo), "儲位基本資料無法修改");

    [HttpDelete("locator-basis/item")]
    public IActionResult DeleteLocatorBasis(
        [FromQuery] string inventoryNo,
        [FromQuery] string locatorNo,
        [FromQuery] int issueState,
        [FromQuery] long dataStamp) =>
        ExecuteAction(() => locatorBasisRepository.Delete(inventoryNo, locatorNo, issueState, dataStamp, ActorUserNo), "儲位基本資料無法刪除");

    [HttpPost("locator-basis/item/approve")]
    public ActionResult<SmdLocatorBasisDto> ApproveLocatorBasis(
        [FromQuery] string inventoryNo,
        [FromQuery] string locatorNo,
        [FromQuery] int issueState,
        [FromQuery] long dataStamp) =>
        Execute(() => locatorBasisRepository.Approve(inventoryNo, locatorNo, issueState, dataStamp), "儲位基本資料尚未接妥簽核服務");

    [HttpGet("factory-basis")]
    public ActionResult<IReadOnlyList<SmdFactoryBasisDto>> FactoryBasis([FromQuery] string? factoryNo) =>
        Execute(() => factoryBasisRepository.List(factoryNo), "工廠基本資料讀取失敗");

    [HttpGet("factory-basis/options")]
    public ActionResult<SmdFactoryBasisOptionsDto> FactoryBasisOptions() =>
        Ok(new SmdFactoryBasisOptionsDto(configuration.GetValue<bool>("YMES_IC_FT_PKG_ENABLED")));

    [HttpPost("factory-basis")]
    public ActionResult<SmdFactoryBasisDto> CreateFactoryBasis([FromBody] SmdFactoryBasisWriteRequest request) =>
        Execute(() => factoryBasisRepository.Create(request, ActorUserNo), "工廠基本資料無法建立");

    [HttpPut("factory-basis/{factoryNo}")]
    public ActionResult<SmdFactoryBasisDto> UpdateFactoryBasis(
        string factoryNo,
        [FromQuery] int issueState,
        [FromQuery] long dataStamp,
        [FromBody] SmdFactoryBasisWriteRequest request) =>
        Execute(() => factoryBasisRepository.Update(factoryNo, issueState, dataStamp, request, ActorUserNo), "工廠基本資料無法修改");

    [HttpDelete("factory-basis/{factoryNo}")]
    public IActionResult DeleteFactoryBasis(string factoryNo, [FromQuery] int issueState, [FromQuery] long dataStamp) =>
        ExecuteAction(() => factoryBasisRepository.Delete(factoryNo, issueState, dataStamp), "工廠基本資料無法刪除");

    [HttpPost("factory-basis/{factoryNo}/approve")]
    public ActionResult<SmdFactoryBasisDto> ApproveFactoryBasis(
        string factoryNo,
        [FromBody] SmdFactoryBasisExpectedRequest request) =>
        Execute(() => factoryBasisRepository.Approve(factoryNo, request, ActorUserNo), "工廠基本資料尚未接妥簽核服務");

    [HttpGet("area-inventories")]
    public ActionResult<IReadOnlyList<SmdAreaInventoryDto>> AreaInventories([FromQuery] string? baseAreaNo) =>
        Execute(() => areaInventoryRepository.ListAreaInventories(baseAreaNo), "區域庫房資料讀取失敗");

    [HttpPost("area-inventories")]
    public ActionResult<SmdAreaInventoryDto> CreateAreaInventory([FromBody] SmdAreaInventoryWriteRequest request) =>
        Execute(() => areaInventoryRepository.CreateAreaInventory(request, ActorUserNo), "區域庫房資料無法建立");

    [HttpPut("area-inventories/{areaNo}/{inventoryNo}")]
    public ActionResult<SmdAreaInventoryDto> UpdateAreaInventory(
        string areaNo,
        string inventoryNo,
        [FromQuery] long dataStamp,
        [FromBody] SmdAreaInventoryUpdateRequest request) =>
        Execute(() => areaInventoryRepository.UpdateAreaInventory(
            areaNo, inventoryNo, request.BaseAreaNo, dataStamp, request, ActorUserNo), "區域庫房資料無法更新");

    [HttpDelete("area-inventories/{areaNo}/{inventoryNo}")]
    public IActionResult DeleteAreaInventory(
        string areaNo,
        string inventoryNo,
        [FromQuery] string? baseAreaNo,
        [FromQuery] long dataStamp) =>
        ExecuteAction(
            () => areaInventoryRepository.DeleteAreaInventory(areaNo, inventoryNo, baseAreaNo, dataStamp),
            "區域庫房資料無法刪除");

    [HttpGet("areas/{baseAreaNo}/layout")]
    public ActionResult<SmdAreaLayoutRelationsDto> AreaLayout(string baseAreaNo) =>
        Execute(() => areaInventoryRepository.ListAreaRelations(baseAreaNo), "區域配置資料讀取失敗");

    [HttpGet("computer-areas")]
    public ActionResult<IReadOnlyList<SmdComputerAreaDto>> ComputerAreas([FromQuery] string? computerNo, [FromQuery] string? factoryNo) =>
        Execute(() => repository.ListComputerAreas(computerNo, factoryNo));

    [HttpPost("computer-areas")]
    public ActionResult<SmdComputerAreaDto> CreateComputerArea([FromBody] SmdComputerAreaWriteRequest request) =>
        Execute(() => repository.CreateComputerArea(request, ActorUserNo), "過帳電腦之區域資料無法建立");

    [HttpPut("computer-areas/{computerNo}/{factoryNo}")]
    public ActionResult<SmdComputerAreaDto> UpdateComputerArea(string computerNo, string factoryNo, [FromQuery] long dataStamp, [FromBody] SmdComputerAreaUpdateRequest request) =>
        Execute(() => repository.UpdateComputerArea(computerNo, factoryNo, dataStamp, request, ActorUserNo), "過帳電腦之區域資料無法更新");

    [HttpDelete("computer-areas/{computerNo}/{factoryNo}")]
    public IActionResult DeleteComputerArea(string computerNo, string factoryNo)
    {
        try
        {
            repository.DeleteComputerArea(computerNo, factoryNo);
            return NoContent();
        }
        catch (MasterDataRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "過帳電腦之區域資料無法刪除", detail: ex.Message);
        }
    }

    [HttpPost("computer-areas/copy")]
    public ActionResult<SmdComputerAreaDto> CopyComputerArea([FromBody] SmdComputerAreaCopyRequest request) =>
        Execute(() => repository.CopyComputerArea(request, ActorUserNo), "過帳電腦之區域資料無法複製");

    [HttpGet("area-basis/options")]
    public ActionResult<SmdAreaBasisOptionsDto> AreaBasisOptions() => Execute(areaBasisRepository.Options);

    [HttpGet("areas")]
    public ActionResult<IReadOnlyList<SmdAreaBasisDto>> Areas(
        [FromQuery] string? areaNo,
        [FromQuery] string? factoryNo,
        [FromQuery] int? issueState,
        [FromQuery] int? areaClass) =>
        Execute(() => areaBasisRepository.ListAreas(areaNo, factoryNo, issueState, areaClass));

    [HttpPost("areas")]
    public ActionResult<SmdAreaBasisDto> CreateArea([FromBody] SmdAreaBasisWriteRequest request) =>
        Execute(() => areaBasisRepository.Create(request, ActorUserNo), "區域資料無法建立");

    [HttpPut("areas/{areaNo}")]
    public ActionResult<SmdAreaBasisDto> UpdateArea(
        string areaNo,
        [FromQuery] long dataStamp,
        [FromBody] SmdAreaBasisWriteRequest request) =>
        Execute(() => areaBasisRepository.Update(areaNo, dataStamp, request, ActorUserNo), "區域資料無法更新");

    [HttpDelete("areas/{areaNo}")]
    public IActionResult DeleteArea(
        string areaNo,
        [FromQuery] int issueState,
        [FromQuery] long dataStamp) =>
        ExecuteAction(() => areaBasisRepository.Delete(areaNo, issueState, dataStamp, ActorUserNo), "區域資料無法刪除");

    [HttpPost("areas/{areaNo}/approve")]
    public ActionResult<SmdAreaBasisDto> ApproveArea(string areaNo, [FromQuery] long dataStamp) =>
        Execute(() => areaBasisRepository.Approve(areaNo, dataStamp), "區域資料尚未接妥簽核服務");

    [HttpGet("areas/{areaNo}/layout-options")]
    public IActionResult AreaLayoutOptions(string areaNo) =>
        Problem(statusCode: 503, title: "區域配置選項尚未備妥", detail: "尚未取得可核對的 MES funSaveAreaData／LoadAreaRelationDefine 選項來源；此端點不回傳推測資料。");

    [HttpPut("areas/{areaNo}/layout")]
    public IActionResult UpdateAreaLayout(string areaNo, [FromQuery] long dataStamp) =>
        Problem(statusCode: 503, title: "區域配置儲存尚未備妥", detail: "尚未取得可核對的 MES funSaveAreaData 關聯推導規則；此端點不會修改正式資料。");

    [HttpGet("pd-line-basis")]
    public ActionResult<IReadOnlyList<SmdPdLineBasisDto>> PdLineBasis([FromQuery] string? pdLineNo) =>
        Execute(() => repository.ListPdLineBasis(pdLineNo), "生產線別資料讀取失敗");

    [HttpGet("pd-line-basis/options")]
    public ActionResult<SmdPdLineBasisOptionsDto> PdLineBasisOptions() =>
        Execute(repository.PdLineBasisOptions, "生產線狀態選項尚未備妥");

    [HttpPost("pd-line-basis")]
    public ActionResult<SmdPdLineBasisDto> CreatePdLineBasis([FromBody] SmdPdLineBasisWriteRequest request) =>
        Execute(() => repository.CreatePdLineBasis(request, ActorUserNo), "生產線別資料無法建立");

    [HttpPut("pd-line-basis/{pdLineNo}")]
    public ActionResult<SmdPdLineBasisDto> UpdatePdLineBasis(string pdLineNo, [FromQuery] int issueState, [FromQuery] long dataStamp, [FromBody] SmdPdLineBasisWriteRequest request) =>
        Execute(() => repository.UpdatePdLineBasis(pdLineNo, issueState, dataStamp, request, ActorUserNo), "生產線別資料無法更新");

    [HttpDelete("pd-line-basis/{pdLineNo}")]
    public IActionResult DeletePdLineBasis(string pdLineNo, [FromQuery] int issueState, [FromQuery] long dataStamp) =>
        ExecuteAction(() => repository.DeletePdLineBasis(pdLineNo, issueState, dataStamp, ActorUserNo), "生產線別資料無法刪除");

    [HttpPost("pd-line-basis/{pdLineNo}/approve")]
    public ActionResult<SmdPdLineBasisDto> ApprovePdLineBasis(string pdLineNo, [FromQuery] int issueState, [FromQuery] long dataStamp) =>
        Execute(() => repository.ApprovePdLineBasis(pdLineNo, issueState, dataStamp, ActorUserNo), "生產線別資料無法核准");

    private string ActorUserNo => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    private ActionResult<T> Execute<T>(Func<T> action, string title = "SMD 資料讀取失敗")
    {
        try { return Ok(action()); }
        catch (MasterDataRuleException ex) { return Problem(statusCode: ex.StatusCode, title: title, detail: ex.Message); }
    }

    private IActionResult ExecuteAction(Action action, string title)
    {
        try { action(); return NoContent(); }
        catch (MasterDataRuleException ex) { return Problem(statusCode: ex.StatusCode, title: title, detail: ex.Message); }
    }
}
