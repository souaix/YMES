using System.Security.Claims;
using MES.Api.Data;
using MES.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MES.Api.Controllers;

[ApiController]
[Route("api/smd")]
public sealed class SmdController(PostgresSmdRepository repository) : ControllerBase
{
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
    public ActionResult<SmdAreaBasisOptionsDto> AreaBasisOptions() => Execute(repository.AreaBasisOptions);

    [HttpGet("areas")]
    public ActionResult<IReadOnlyList<SmdAreaOptionDto>> Areas([FromQuery] string? factoryNo, [FromQuery] int? issueState, [FromQuery] int? areaClass) =>
        Execute(() => repository.ListAreas(factoryNo, issueState, areaClass));

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
