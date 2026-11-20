using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Enterprise;

[ApiController]
[Authorize]
[Route("api/ent/customer-basis")]
public sealed class Et101Controller(Et101Repository repository, ILogger<Et101Controller> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";
    [HttpGet]
    public IActionResult List([FromQuery] string? keyword = null) => Execute(() => Ok(new { items = repository.List(keyword) }));
    [HttpGet("item")]
    public IActionResult Item([FromQuery] string customerNo) => Execute(() => Ok(repository.Get(customerNo)));
    [HttpPost]
    public IActionResult Create([FromBody] Et101WriteRequest request) => Execute(() => Ok(repository.Create(request, Actor)));
    [HttpPut("item")]
    public IActionResult Update([FromQuery] string customerNo, [FromBody] Et101UpdateRequest request) =>
        Execute(() => Ok(repository.Update(customerNo, request, Actor)));
    [HttpPost("approve")]
    public IActionResult Approve([FromQuery] string customerNo, [FromBody] Et101ExpectedRequest request) =>
        Execute(() => Ok(repository.Approve(customerNo, request, Actor)));
    [HttpDelete("item")]
    public IActionResult Delete([FromQuery] string customerNo, [FromBody] Et101ExpectedRequest request) =>
        Execute(() => Ok(repository.Delete(customerNo, request, Actor)));
    [HttpGet("options/brokers")]
    public IActionResult Brokers() => Execute(() => Ok(new { items = repository.Brokers() }));
    [HttpGet("options/forwarders")]
    public IActionResult Forwarders() => Execute(() => Ok(new { items = repository.Forwarders() }));

    [HttpPost("import/preview")]
    [HttpPost("import/commit")]
    [HttpGet("import/template")]
    public IActionResult ImportUnavailable() => Problem(statusCode: 503, title: "客戶匯入服務尚未提供",
        detail: "原生 CustomerDataChecker 與 ENT BASIS 範本服務尚未接上，未執行匯入。");

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Et101RuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "客戶操作失敗", detail: ex.Message); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Problem(statusCode: 409, title: "客戶識別碼重複", detail: "客戶已存在；原資料未接管，請重新查詢。"); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("ET101 database operation failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: 503, title: "客戶服務暫時無法使用",
                detail: "必要的客戶結構、引用來源或權限尚未就緒；未提供替代資料。");
        }
    }
}
