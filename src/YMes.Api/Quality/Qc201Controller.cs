using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/attribute-qc-items")]
public sealed class Qc201Controller(Qc201Repository repository, ILogger<Qc201Controller> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] int? issueState, [FromQuery] string? search) => Execute(() => Ok(new { items=repository.List(issueState,search) }));
    [HttpGet("item")]
    public IActionResult Item([FromQuery] string qcItemNo) => Execute(() => Ok(repository.Get(qcItemNo)));
    [HttpPost]
    public IActionResult Create([FromBody] Qc201ItemWriteRequest request) => Execute(() => Ok(repository.Create(request,Actor)));
    [HttpPut("item")]
    public IActionResult Update([FromQuery] string qcItemNo,[FromBody] Qc201UpdateRequest request) => Execute(() => Ok(repository.Update(qcItemNo,request,Actor)));
    [HttpDelete("item")]
    public IActionResult Delete([FromQuery] string qcItemNo,[FromBody] Qc201DeleteRequest request) => Execute(() => { repository.Delete(qcItemNo,request,Actor);return NoContent(); });
    [HttpPost("copy-preview")]
    public IActionResult CopyPreview([FromQuery] string qcItemNo,[FromBody] Qc201CopyWriteRequest request) => Execute(() => Ok(repository.CopyPreview(qcItemNo,request)));
    [HttpPost("copy")]
    public IActionResult Copy([FromQuery] string qcItemNo,[FromBody] Qc201CopyWriteRequest request) => Execute(() => Ok(repository.Copy(qcItemNo,request,Actor)));
    [HttpPost("approve")]
    public IActionResult Approve([FromQuery] string qcItemNo,[FromBody] Qc201ApproveRequest request) => Execute(() => Ok(repository.Approve(qcItemNo,request,Actor)));

    [HttpGet("options")]
    public IActionResult Options([FromQuery] int? componentCollect=0) => Execute(() => Ok(repository.GetOptions(componentCollect)));
    [HttpGet("options/reasons")]
    public IActionResult Reasons([FromQuery] int issueState=2) => Execute(() => { if(issueState!=2)throw new Qc201RuleException(400,"原因查找僅提供IssueState=2。");return Ok(new { items=repository.ApprovedReasons() }); });
    [HttpGet("options/reason-types")]
    public IActionResult ReasonTypes() => Execute(() => Ok(new { items=repository.ReasonTypeOptions() }));
    [HttpGet("options/interfaces")]
    public IActionResult Interfaces() => Execute(() => Ok(new { items=repository.Interfaces() }));
    [HttpGet("options/factor-values")]
    public IActionResult FactorReference([FromQuery] string factorNo) => Execute(() => Ok(repository.FactorReference(factorNo)));
    [HttpGet("options/factors")]
    public IActionResult FactorChoices() => Execute(() => Ok(new { items=repository.FactorChoices() }));
    [HttpGet("options/operations")]
    public IActionResult OperationChoices([FromQuery] int qcType=0,[FromQuery] int lotIsValid=0) => Execute(() => Ok(new { items=repository.ApprovedOperationChoices(qcType,lotIsValid) }));

    [HttpGet("parameters")]
    public IActionResult Parameters([FromQuery] string qcItemNo) => Execute(() => Ok(repository.GetParameters(qcItemNo)));
    [HttpPut("parameters")]
    public IActionResult SaveParameters([FromQuery] string qcItemNo,[FromBody] Qc201RowsRequest<Qc201ParameterRow> request) => Execute(() => Ok(repository.SaveParameters(qcItemNo,request,Actor)));
    [HttpGet("factors")]
    public IActionResult Factors([FromQuery] string qcItemNo,[FromQuery] string? factorOnline=null) => Execute(() => Ok(repository.GetFactors(qcItemNo,factorOnline)));
    [HttpPut("factors")]
    public IActionResult SaveFactors([FromQuery] string qcItemNo,[FromBody] Qc201RowsRequest<Qc201FactorRow> request) => Execute(() => Ok(repository.SaveFactors(qcItemNo,request,Actor)));
    [HttpGet("reasons")]
    public IActionResult ItemReasons([FromQuery] string qcItemNo) => Execute(() => Ok(repository.GetReasonMaps(qcItemNo)));
    [HttpPut("reasons")]
    public IActionResult SaveReasons([FromQuery] string qcItemNo,[FromBody] Qc201RowsRequest<Qc201ReasonRow> request) => Execute(() => Ok(repository.SaveReasons(qcItemNo,request,Actor)));
    [HttpGet("operations")]
    public IActionResult Operations([FromQuery] string qcItemNo) => Execute(() => Ok(repository.GetOperations(qcItemNo)));
    [HttpPut("operations")]
    public IActionResult SaveOperations([FromQuery] string qcItemNo,[FromBody] Qc201RowsRequest<Qc201OperationRow> request) => Execute(() => Ok(repository.SaveOperations(qcItemNo,request,Actor)));
    [HttpGet("samplePlanChanges")]
    public IActionResult SamplePlanChanges([FromQuery] string qcItemNo) => Execute(() => Ok(repository.GetSamplePlanChanges(qcItemNo)));
    [HttpPut("samplePlanChanges")]
    public IActionResult SaveSamplePlanChanges([FromQuery] string qcItemNo,[FromBody] Qc201RowsRequest<Qc201SamplePlanChangeRow> request) => Execute(() => Ok(repository.SaveSamplePlanChanges(qcItemNo,request,Actor)));
    [HttpPost("simulate")]
    public IActionResult Simulate([FromQuery] string qcItemNo,[FromBody] Qc201SimulationRequest request) => Execute(() => Ok(repository.Simulation(qcItemNo,request)));

    // Read compatibility only; new UI writes use query keys so slash/pipe/star
    // remain literal identifiers rather than route segments.
    [HttpGet("{qcItemNo}/factors")]
    public IActionResult LegacyFactors(string qcItemNo,[FromQuery] string? factorOnline=null) => Execute(() => Ok(repository.GetFactors(qcItemNo,factorOnline)));
    [HttpGet("{qcItemNo}/parameters")]
    public IActionResult LegacyParameters(string qcItemNo) => Execute(() => Ok(repository.GetParameters(qcItemNo)));
    [HttpGet("{qcItemNo}/reasons")]
    public IActionResult LegacyReasons(string qcItemNo) => Execute(() => Ok(repository.GetReasonMaps(qcItemNo)));
    [HttpGet("{qcItemNo}/operations")]
    public IActionResult LegacyOperations(string qcItemNo) => Execute(() => Ok(repository.GetOperations(qcItemNo)));
    [HttpGet("{qcItemNo}/samplePlanChanges")]
    public IActionResult LegacyPlanChanges(string qcItemNo) => Execute(() => Ok(repository.GetSamplePlanChanges(qcItemNo)));

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch(Qc201RuleException ex) { return Problem(statusCode:ex.StatusCode,title:"計數品管項目操作失敗",detail:ex.Message); }
        catch(PostgresException ex) when(ex.SqlState==PostgresErrorCodes.UniqueViolation)
        { return Problem(statusCode:409,title:"計數品管項目資料重複",detail:"編號或明細複合鍵已存在，請重新查詢。"); }
        catch(NpgsqlException ex)
        {
            logger.LogError("QC201 database access failed for {Actor}: {ErrorType}",Actor,ex.GetType().Name);
            return Problem(statusCode:503,title:"計數品管項目服務暫時無法使用",detail:"無法存取主檔、明細、鎖定權限或真實目錄，請重新查詢確認資料；不以缺少的相依來源當作空設定。");
        }
    }
}
