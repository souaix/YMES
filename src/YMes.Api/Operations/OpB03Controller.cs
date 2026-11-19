using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Operations;

[ApiController]
[Authorize]
[Route("api/customer-op-mappings")]
public sealed class OpB03Controller(OpB03Repository repository, ILogger<OpB03Controller> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? customerNo = null, [FromQuery] string? customerOPNo = null) =>
        Execute(() => Ok(new { items = repository.List(customerNo, customerOPNo) }));
    [HttpGet("item")]
    public IActionResult Item([FromQuery] string customerNo, [FromQuery] string customerOPNo) =>
        Execute(() => Ok(repository.Get(customerNo, customerOPNo)));
    [HttpPost]
    public IActionResult Create([FromBody] OpB03HeaderWriteRequest request) =>
        Execute(() => Ok(repository.Create(request, Actor)));
    [HttpPut("item")]
    public IActionResult Update([FromQuery] string customerNo, [FromQuery] string customerOPNo,
        [FromBody] OpB03HeaderUpdateRequest request) =>
        Execute(() => Ok(repository.Update(customerNo, customerOPNo, request, Actor)));
    [HttpDelete("item")]
    public IActionResult Delete([FromQuery] string customerNo, [FromQuery] string customerOPNo,
        [FromBody] OpB03ExpectedRequest request) => Execute(() =>
        { repository.Delete(customerNo, customerOPNo, request, Actor); return NoContent(); });

    [HttpGet("details")]
    public IActionResult Details([FromQuery] string customerNo, [FromQuery] string customerOPNo) =>
        Execute(() => Ok(repository.Details(customerNo, customerOPNo)));
    [HttpPost("details")]
    public IActionResult AddDetail([FromQuery] string customerNo, [FromQuery] string customerOPNo,
        [FromBody] OpB03DetailWriteRequest request) =>
        Execute(() => Ok(repository.AddDetail(customerNo, customerOPNo, request, Actor)));
    [HttpPut("details/item")]
    public IActionResult UpdateDetail([FromQuery] string customerNo, [FromQuery] string customerOPNo,
        [FromQuery] string opNo, [FromBody] OpB03DetailUpdateRequest request) =>
        Execute(() => Ok(repository.UpdateDetail(customerNo, customerOPNo, opNo, request, Actor)));
    [HttpDelete("details/item")]
    public IActionResult DeleteDetail([FromQuery] string customerNo, [FromQuery] string customerOPNo,
        [FromQuery] string opNo, [FromBody] OpB03DetailExpectedRequest request) =>
        Execute(() => Ok(repository.DeleteDetail(customerNo, customerOPNo, opNo, request, Actor)));

    [HttpGet("lookups/customers")]
    public IActionResult Customers() => Execute(() => Ok(new { items = repository.Customers() }));
    [HttpGet("lookups/production-segments")]
    public IActionResult Segments() => Execute(() => Ok(new { items = repository.Segments() }));
    [HttpGet("lookups/operations")]
    public IActionResult Operations([FromQuery] string psNo) =>
        Execute(() => Ok(new { items = repository.Operations(psNo) }));

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (OpB03RuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "客戶站點對應操作失敗", detail: ex.Message); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Problem(statusCode: 409, title: "客戶站點對應資料重複", detail: "指定客戶站點或該對應下的作業站已存在，請重新查詢。"); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("OPB03 database operation failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: 503, title: "客戶站點對應服務暫時無法使用",
                detail: "無法存取主檔、明細或真實客戶／區段／作業站來源，請確認結構與必要權限已部署。");
        }
    }
}
