using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Enterprise;

[ApiController]
[Authorize]
[Route("api/ent/customer-contactors")]
public sealed class Et102Controller(Et102Repository repository, ILogger<Et102Controller> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet("lookups/customers")]
    public IActionResult Customers() => Execute(() => Ok(new { items = repository.Customers() }));
    [HttpGet]
    public IActionResult List([FromQuery] string customerNo) =>
        Execute(() => Ok(new { items = repository.List(customerNo) }));
    [HttpGet("item")]
    public IActionResult Item([FromQuery] string customerNo, [FromQuery] string contactorName) =>
        Execute(() => Ok(repository.Get(customerNo, contactorName)));
    [HttpPost]
    public IActionResult Create([FromQuery] string customerNo, [FromBody] Et102ContactorWriteRequest request) =>
        Execute(() => Ok(repository.Create(customerNo, request, Actor)));
    [HttpPut("item")]
    public IActionResult Update([FromQuery] string customerNo, [FromQuery] string contactorName,
        [FromBody] Et102ContactorUpdateRequest request) =>
        Execute(() => Ok(repository.Update(customerNo, contactorName, request, Actor)));
    [HttpDelete("item")]
    public IActionResult Delete([FromQuery] string customerNo, [FromQuery] string contactorName,
        [FromBody] Et102ExpectedRequest request) => Execute(() =>
        { repository.Delete(customerNo, contactorName, request, Actor); return NoContent(); });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Et102RuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "客戶連絡人操作失敗", detail: ex.Message); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Problem(statusCode: 409, title: "客戶連絡人資料重複", detail: "此客戶已存在相同連絡人，請重新查詢。"); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("ET102 database operation failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: 503, title: "客戶連絡人服務暫時無法使用",
                detail: "無法存取真實客戶或連絡人資料，請確認必要結構及權限已部署。");
        }
    }
}
