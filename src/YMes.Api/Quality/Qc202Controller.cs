using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/attribute-control-limits")]
public sealed class Qc202Controller(Qc202Repository repository, ILogger<Qc202Controller> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string qcItemNo = "") => Execute(() => Ok(new { items = repository.List(qcItemNo) }));

    [HttpGet("options")]
    public IActionResult Options([FromQuery] string qcItemNo = "") => Execute(() => Ok(repository.Options(qcItemNo)));

    [HttpPost]
    public IActionResult Create([FromBody] Qc202ControlLimitWriteRequest request) => Execute(() => Ok(repository.Create(request, Actor)));

    [HttpPut("item")]
    public IActionResult Update([FromQuery] string qcItemNo, [FromBody] Qc202UpdateRequest request) => Execute(() => Ok(repository.Update(qcItemNo, request, Actor)));

    [HttpDelete("item")]
    public IActionResult Delete([FromQuery] string qcItemNo, [FromBody] Qc202DeleteRequest request) => Execute(() =>
    {
        repository.Delete(qcItemNo, request);
        return NoContent();
    });

    // Existing simple-key clients remain compatible. New clients use query
    // keys so literal slash/pipe/star identifiers are never path segments.
    [HttpPut("{qcItemNo}")]
    public IActionResult LegacyUpdate(string qcItemNo, [FromBody] Qc202UpdateRequest request) => Execute(() => Ok(repository.Update(qcItemNo, request, Actor)));

    [HttpDelete("{qcItemNo}")]
    public IActionResult LegacyDelete(string qcItemNo, [FromBody] Qc202DeleteRequest request) => Execute(() =>
    {
        repository.Delete(qcItemNo, request);
        return NoContent();
    });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc202RuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "QC202 資料操作失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning(ex, "QC202 database operation failed");
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "QC202 資料操作失敗",
                detail: "目前無法完成資料操作，請確認 YMES 資料庫及相依功能已部署後重試。");
        }
    }
}
