using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/attribute-item-groups")]
public sealed class Qc213Controller(NpgsqlDataSource dataSource, ILogger<Qc213Controller> logger) : ControllerBase
{
    private readonly Qc213Repository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? functionNo, [FromQuery] int? issueState, [FromQuery] string? groupNo) =>
        Execute("List", null, () => Ok(new { items = repository.ListGroups(functionNo, issueState, groupNo) }));

    [HttpGet("{groupNo}/details")]
    public IActionResult Details(string groupNo) =>
        Execute("Details", groupNo, () => Ok(repository.Details(groupNo)));

    [HttpPost]
    public IActionResult Create([FromBody] Qc213GroupWriteRequest request) =>
        Execute("Create", request.GroupNo, () => Ok(repository.Create(request, Actor)));

    [HttpPut("{groupNo}")]
    public IActionResult Update(string groupNo, [FromBody] Qc213GroupUpdateRequest request) =>
        Execute("Update", groupNo, () => Ok(repository.Update(groupNo, request, Actor)));

    [HttpDelete("{groupNo}")]
    public IActionResult Delete(string groupNo, [FromBody] Qc213CommandRequest request) =>
        Execute("Delete", groupNo, () =>
        {
            repository.Delete(groupNo, request, Actor);
            return NoContent();
        });

    [HttpPost("{groupNo}/approval")]
    public IActionResult Approve(string groupNo, [FromBody] Qc213CommandRequest request) =>
        Execute("Approve", groupNo, () => Ok(repository.Approve(groupNo, request, Actor)));

    [HttpPut("{groupNo}/details")]
    public IActionResult ReplaceDetails(string groupNo, [FromBody] Qc213DetailReplaceRequest request) =>
        Execute("ReplaceDetails", groupNo, () =>
        {
            repository.ReplaceDetails(groupNo, request);
            return NoContent();
        });

    private IActionResult Execute(string action, string? subject, Func<IActionResult> operation)
    {
        try
        {
            var result = operation();
            logger.LogInformation("QC213 {Action} completed for {GroupNo} by {Actor}", action, subject, Actor);
            return result;
        }
        catch (Qc213RuleException ex)
        {
            logger.LogWarning("QC213 {Action} rejected for {GroupNo} by {Actor}: {Reason}", action, subject, Actor, ex.Message);
            return Problem(statusCode: ex.StatusCode, title: "計數品管項目群組操作失敗", detail: ex.Message);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.SerializationFailure)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "計數品管項目群組資料已變更", detail: "資料鍵或版本已被其他作業使用，請重新查詢後再試。");
        }
        catch (NpgsqlException ex)
        {
            logger.LogError("QC213 {Action} failed for {GroupNo} by {Actor}; error {ErrorType}", action, subject, Actor, ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "QC213 資料服務暫時無法使用", detail: "YMES 資料庫操作失敗，請確認 QC213 相依表與 SELECT 權限已部署。");
        }
    }
}

[ApiController]
[Authorize]
[Route("api/qc/item-operations")]
public sealed class Qc213ItemOperationsController(NpgsqlDataSource dataSource, ILogger<Qc213ItemOperationsController> logger) : ControllerBase
{
    private readonly Qc213Repository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] int? qcItemType, [FromQuery] string? qcItemNo) =>
        Execute("ListOperations", qcItemNo, () => Ok(repository.Operations(qcItemType, qcItemNo)));

    [HttpPut]
    public IActionResult Replace([FromQuery] int? qcItemType, [FromQuery] string? qcItemNo, [FromBody] Qc213OperationReplaceRequest request) =>
        Execute("ReplaceOperations", qcItemNo, () =>
        {
            repository.ReplaceOperations(qcItemType, qcItemNo, request, Actor);
            return NoContent();
        });

    private IActionResult Execute(string action, string? subject, Func<IActionResult> operation)
    {
        try
        {
            var result = operation();
            logger.LogInformation("QC213 {Action} completed for {GroupNo} by {Actor}", action, subject, Actor);
            return result;
        }
        catch (Qc213RuleException ex)
        {
            logger.LogWarning("QC213 {Action} rejected for {GroupNo} by {Actor}: {Reason}", action, subject, Actor, ex.Message);
            return Problem(statusCode: ex.StatusCode, title: "QC213 作業站對應操作失敗", detail: ex.Message);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.SerializationFailure)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "QC213 作業站對應資料已變更", detail: "資料鍵或版本已被其他作業使用，請重新查詢後再試。");
        }
        catch (NpgsqlException ex)
        {
            logger.LogError("QC213 {Action} failed for {GroupNo} by {Actor}; error {ErrorType}", action, subject, Actor, ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "QC213 作業站服務暫時無法使用", detail: "YMES 資料庫操作失敗，請確認共用作業站表與 SELECT 權限已部署。");
        }
    }
}
