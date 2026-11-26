using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/item-groups")]
public sealed class Qc212Controller(NpgsqlDataSource dataSource, ILogger<Qc212Controller> logger) : ControllerBase
{
    private readonly Qc212Repository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? functionNo, [FromQuery] int? issueState, [FromQuery] string? groupNo) =>
        Execute(() => Ok(new { items = repository.ListGroups(functionNo, issueState, groupNo) }));

    [HttpGet("{groupNo}/details")]
    public IActionResult Details(string groupNo) => Execute(() => Ok(repository.Details(groupNo)));

    [HttpPost]
    public IActionResult Create([FromBody] Qc212GroupWriteRequest request) => Write("Create", request.GroupNo,
        () => Ok(repository.Create(request, Actor)));

    [HttpPut("{groupNo}")]
    public IActionResult Update(string groupNo, [FromBody] Qc212GroupUpdateRequest request) => Write("Update", groupNo,
        () => Ok(repository.Update(groupNo, request, Actor)));

    [HttpDelete("{groupNo}")]
    public IActionResult Delete(string groupNo) => Write("Delete", groupNo, () =>
    {
        repository.Delete(groupNo, Actor);
        return NoContent();
    });

    [HttpPost("{groupNo}/approve")]
    public IActionResult Approve(string groupNo, [FromBody] Qc212ApproveRequest request) => Write("Approve", groupNo,
        () => Ok(repository.Approve(groupNo, request, Actor)));

    [HttpPost("{sourceGroupNo}/copy-details")]
    public IActionResult CopyDetails(string sourceGroupNo, [FromBody] Qc212CopyDetailsRequest request) => Write("CopyDetails", sourceGroupNo,
        () =>
        {
            repository.CopyDetails(sourceGroupNo, request);
            return NoContent();
        });

    [HttpPut("{groupNo}/details")]
    public IActionResult ReplaceDetails(string groupNo, [FromBody] Qc212DetailReplaceRequest request) => Write("ReplaceDetails", groupNo,
        () =>
        {
            repository.ReplaceDetails(groupNo, request);
            return NoContent();
        });

    [HttpDelete("{groupNo}/details/{qcItemNo}")]
    public IActionResult RemoveDetail(string groupNo, string qcItemNo) => Write("RemoveDetail", groupNo,
        () =>
        {
            repository.RemoveDetail(groupNo, qcItemNo);
            return NoContent();
        });

    [HttpGet("{groupNo}/parameters")]
    public IActionResult Parameters(string groupNo, [FromQuery] int? planType = null) => Execute(() => Ok(repository.Parameters(groupNo)));

    [HttpPut("{groupNo}/parameters")]
    public IActionResult ReplaceParameters(string groupNo, [FromBody] List<Qc212ParameterWriteRequest>? request) => Write("ReplaceParameters", groupNo,
        () =>
        {
            repository.ReplaceParameters(groupNo, request);
            return NoContent();
        });

    [HttpGet("{groupNo}/operations")]
    public IActionResult Operations(string groupNo) => Execute(() => Ok(repository.OperationsForGroup(groupNo)));

    [HttpPut("{groupNo}/operations")]
    public IActionResult ReplaceOperations(string groupNo, [FromBody] Qc212OperationsWriteRequest request) => Write("ReplaceOperations", groupNo,
        () =>
        {
            repository.ReplaceOperations(groupNo, request, Actor);
            return NoContent();
        });

    [HttpGet("{groupNo}/details/{qcItemNo}/script")]
    public IActionResult Script(string groupNo, string qcItemNo) => Execute(() => Ok(repository.Script(groupNo, qcItemNo)));

    [HttpPut("{groupNo}/details/{qcItemNo}/script")]
    public IActionResult SaveScript(string groupNo, string qcItemNo, [FromBody] Qc212ScriptWriteRequest request) => Write("SaveScript", groupNo,
        () => Ok(repository.SaveScript(groupNo, qcItemNo, request, Actor)));

    [HttpGet("{groupNo}/memo-fields")]
    public IActionResult MemoFields(string groupNo) => Execute(() => Ok(repository.MemoFields(groupNo)));

    [HttpPut("{groupNo}/memo-fields")]
    public IActionResult ReplaceMemoFields(string groupNo, [FromBody] Qc212MemoWriteRequest request) => Write("ReplaceMemoFields", groupNo,
        () =>
        {
            repository.ReplaceMemoFields(groupNo, request, Actor);
            return NoContent();
        });

    [HttpGet("options/winspc-interfaces")]
    public IActionResult WinSpcInterfaces() => Execute(() => Ok(repository.WinSpcInterfaces()));

    [HttpGet("options/qc-classes")]
    public IActionResult QcClasses() => Execute(() => Ok(repository.QcClasses()));

    private IActionResult Write(string action, string subject, Func<IActionResult> operation)
    {
        try
        {
            var result = operation();
            logger.LogInformation("QC212 {Action} completed for {GroupNo} by {Actor}", action, subject, Actor);
            return result;
        }
        catch (Qc212RuleException ex)
        {
            logger.LogWarning("QC212 {Action} rejected for {GroupNo} by {Actor}: {Reason}", action, subject, Actor, ex.Message);
            return Problem(statusCode: ex.StatusCode, title: "計量品管項目群組操作失敗", detail: ex.Message);
        }
        catch (PostgresException ex) when (ex.SqlState is "23505" or "40001")
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "計量品管項目群組資料已變更", detail: "資料鍵或版本已被其他作業使用，請重新查詢後再試。");
        }
        catch (NpgsqlException ex)
        {
            logger.LogError("QC212 {Action} failed for {GroupNo} by {Actor}; error {ErrorType}", action, subject, Actor, ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "QC212 資料服務暫時無法使用", detail: "YMES 資料庫操作失敗，請確認 QC212 相依表與權限已部署。");
        }
    }

    private IActionResult Execute(Func<IActionResult> operation)
    {
        try { return operation(); }
        catch (Qc212RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "計量品管項目群組查詢失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogError("QC212 read failed for {Actor}; error {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "QC212 資料服務暫時無法使用", detail: "YMES 資料庫查詢失敗，請確認 QC212 相依表與 SELECT 權限已部署。");
        }
    }
}
