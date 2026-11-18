using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc")]
public sealed class Qc501Qc502Controller(NpgsqlDataSource dataSource, ILogger<Qc501Qc502Controller> logger) : ControllerBase
{
    private readonly Qc501Qc502Repository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet("inspection-machine-parameter-types")]
    public IActionResult ListTypes([FromQuery] int? issueState = null, [FromQuery] string? inspMachineType = null) =>
        Execute(() => Ok(new { items = issueState.HasValue
            ? repository.ListTypes(issueState.Value, inspMachineType)
            : repository.TypeLookup() }));

    [HttpGet("inspection-machine-parameter-types/{inspMachineType}")]
    public IActionResult GetType(string inspMachineType) => Execute(() => Ok(repository.GetType(inspMachineType)));

    [HttpPost("inspection-machine-parameter-types")]
    public IActionResult CreateType([FromBody] Qc501MachineTypeWriteRequest request) =>
        Execute(() => Ok(repository.CreateType(request, Actor)));

    [HttpPut("inspection-machine-parameter-types/{inspMachineType}")]
    public IActionResult UpdateType(string inspMachineType, [FromBody] Qc501MachineTypeWriteRequest request) =>
        Execute(() => Ok(repository.UpdateType(inspMachineType, request, Actor)));

    [HttpDelete("inspection-machine-parameter-types/{inspMachineType}")]
    public IActionResult DeleteType(string inspMachineType, [FromQuery] int issueState, [FromQuery] decimal? dataStamp) =>
        Execute(() => Ok(repository.DeleteType(inspMachineType, issueState, dataStamp)));

    [HttpPost("inspection-machine-parameter-types/{inspMachineType}/approve")]
    public IActionResult ApproveType(string inspMachineType, [FromBody] QcInspectionStateRequest request) =>
        Execute(() => Ok(repository.ApproveType(inspMachineType, request, Actor)));

    [HttpGet("inspection-machines")]
    public IActionResult ListMachines([FromQuery] int issueState = 0, [FromQuery] string? inspMachineNo = null) =>
        Execute(() => Ok(new { items = repository.ListMachines(issueState, inspMachineNo) }));

    [HttpGet("inspection-machines/{inspMachineNo}")]
    public IActionResult GetMachine(string inspMachineNo) => Execute(() => Ok(repository.GetMachine(inspMachineNo)));

    [HttpGet("inspection-machine-parameter-types/lookup")]
    public IActionResult TypeLookup() => Execute(() => Ok(new { items = repository.TypeLookup() }));

    [HttpGet("inspection-drivers")]
    public IActionResult DriverLookup() => Execute(() => Ok(new { items = repository.DriverLookup() }));

    [HttpPost("inspection-machines")]
    public IActionResult CreateMachine([FromBody] Qc502InspectionMachineWriteRequest request) =>
        Execute(() => Ok(repository.CreateMachine(request, Actor)));

    [HttpPut("inspection-machines/{inspMachineNo}")]
    public IActionResult UpdateMachine(string inspMachineNo, [FromBody] Qc502InspectionMachineWriteRequest request) =>
        Execute(() => Ok(repository.UpdateMachine(inspMachineNo, request, Actor)));

    [HttpDelete("inspection-machines/{inspMachineNo}")]
    public IActionResult DeleteMachine(string inspMachineNo, [FromQuery] int issueState, [FromQuery] decimal? dataStamp) =>
        Execute(() => Ok(repository.DeleteMachine(inspMachineNo, issueState, dataStamp)));

    [HttpPost("inspection-machines/{inspMachineNo}/approve")]
    public IActionResult ApproveMachine(string inspMachineNo, [FromBody] QcInspectionStateRequest request) =>
        Execute(() => Ok(repository.ApproveMachine(inspMachineNo, request, Actor)));

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (QcInspectionRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "QC501/QC502 操作失敗", detail: ex.Message);
        }
        catch (PostgresException)
        {
            logger.LogError("QC501/QC502 PostgreSQL operation failed for {Actor}", Actor);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "QC501/QC502 資料操作失敗", detail: "YMES 資料庫操作失敗，請確認檢測儀器共用資料表與必要資源已部署。");
        }
    }
}
