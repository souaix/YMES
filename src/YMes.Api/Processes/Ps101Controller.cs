using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Processes;

[ApiController]
[Authorize]
[Route("api/production-segments")]
public sealed class Ps101Controller(NpgsqlDataSource dataSource, IConfiguration configuration, ILogger<Ps101Controller> logger) : ControllerBase
{
    private readonly Ps101Repository repository = new(dataSource, configuration["YMES_ENTERPRISE_NO"]);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? psNo = null) =>
        Execute(() => Ok(new { items = repository.List(psNo) }));

    [HttpPost]
    public IActionResult Create([FromBody] Ps101SegmentWriteRequest request) =>
        Execute(() => Ok(repository.Create(request, Actor)));

    [HttpPut("{psNo}")]
    public IActionResult Update(string psNo, [FromBody] Ps101SegmentUpdateRequest request) =>
        Execute(() => Ok(repository.Update(psNo, request, Actor)));

    [HttpDelete("{psNo}")]
    public IActionResult Delete(string psNo, [FromBody] Ps101SegmentStateRequest request) =>
        Execute(() => Ok(repository.Delete(psNo, request, Actor)));

    [HttpPost("{psNo}/approve")]
    public IActionResult Approve(string psNo, [FromBody] Ps101SegmentStateRequest request) =>
        Execute(() => Ok(repository.Approve(psNo, request, Actor)));

    // Shared SYS lookup route; PS101 uses the same table as other serial-rule callers.
    [HttpGet("/api/system/serial-rule-basis")]
    public IActionResult SerialRules([FromQuery] string serialType = "LOTNO") =>
        Execute(() => Ok(new { items = repository.SerialRules(serialType) }));

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Ps101RuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "生產區段操作失敗", detail: ex.Message);
        }
        catch (PostgresException ex)
        {
            logger.LogError("PS101 PostgreSQL operation failed ({SqlState}) for {Actor}", ex.SqlState, Actor);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "生產區段資料操作失敗", detail: "YMES 資料庫操作失敗，請確認生產區段與序號規則資料表已部署。");
        }
        catch (NpgsqlException)
        {
            logger.LogError("PS101 database connection unavailable for {Actor}", Actor);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "生產區段服務暫時無法使用", detail: "目前無法連線至 YMES 資料庫，請稍後再試。");
        }
    }
}
