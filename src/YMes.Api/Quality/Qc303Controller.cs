using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/spec-bases")]
public sealed class Qc303Controller(NpgsqlDataSource dataSource, ILogger<Qc303Controller> logger) : ControllerBase
{
    private readonly Qc303Repository _repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet("qc-items")]
    public IActionResult QcItems() => Read(() => Ok(new { items = _repository.QcItems() }));

    [HttpGet]
    public IActionResult List([FromQuery] string qcItemNo, [FromQuery] int issueState) =>
        Read(() => Ok(new { items = _repository.List(qcItemNo, issueState) }));

    [HttpGet("{serial}/details")]
    public IActionResult Details(string serial) => Read(() => Ok(new { items = _repository.Details(serial) }));

    [HttpGet("factors/{factorNo}/options")]
    public IActionResult FactorOptions(string factorNo, [FromQuery] string qcItemNo) =>
        Read(() => Ok(new { items = _repository.FactorOptions(qcItemNo, factorNo) }));

    [HttpPost]
    public IActionResult Create([FromBody] Qc303SpecWriteRequest request) =>
        Mutate("create", request.QcItemNo, () => Ok(_repository.Create(request, Actor)));

    [HttpPut("{serial}")]
    public IActionResult Update(string serial, [FromBody] Qc303SpecWriteRequest request) =>
        Mutate("update", serial, () => Ok(_repository.Update(serial, request, Actor)));

    [HttpDelete("{serial}")]
    public IActionResult Delete(string serial, [FromBody] Qc303SpecStateRequest request) =>
        Mutate("delete", serial, () => Ok(_repository.Delete(serial, request, Actor)));

    [HttpPost("{serial}/approve")]
    public IActionResult Approve(string serial, [FromBody] Qc303SpecStateRequest request) =>
        Mutate("approve", serial, () => Ok(_repository.Approve(serial, request, Actor)));

    [HttpPost("{serial}/disapprove")]
    public IActionResult Disapprove(string serial, [FromBody] Qc303SpecStateRequest request) =>
        Mutate("disapprove", serial, () => Ok(_repository.Disapprove(serial, request, Actor)));

    [HttpPost("copy")]
    public IActionResult Copy([FromBody] Qc303CopyRequest request) =>
        Mutate("copy", request.FromQcItemNo, () => Ok(_repository.Copy(request, Actor)));

    private IActionResult Mutate(string actionName, string subject, Func<IActionResult> action)
    {
        try
        {
            var result = action();
            logger.LogInformation("QC303 {Action} completed for {Subject} by {Actor}", actionName, subject, Actor);
            return result;
        }
        catch (Qc303RuleException ex)
        {
            logger.LogWarning("QC303 {Action} rejected for {Subject} by {Actor}: {Reason}", actionName, subject, Actor, ex.Message);
            return Problem(statusCode: ex.StatusCode, title: "QC303 品管規格操作失敗", detail: ex.Message);
        }
        catch (PostgresException)
        {
            logger.LogError("QC303 {Action} failed in PostgreSQL for {Subject} by {Actor}", actionName, subject, Actor);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "QC303 資料操作失敗", detail: "YMES 資料庫操作失敗；交易已取消，請確認資料表與共用資源已部署。");
        }
    }

    private IActionResult Read(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc303RuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "QC303 品管規格查詢失敗", detail: ex.Message);
        }
        catch (PostgresException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "QC303 資料查詢失敗", detail: "YMES 資料庫查詢失敗，請確認資料表與共用資源已部署。");
        }
    }
}
