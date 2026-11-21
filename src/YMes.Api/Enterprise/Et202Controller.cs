using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Enterprise;

[ApiController]
[Authorize]
[Route("api/ent/os-items")]
public sealed class Et202Controller(Et202Repository repository, ILogger<Et202Controller> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? osItemNo = null) =>
        Execute(() => Ok(new { items = repository.List(osItemNo) }));

    [HttpGet("item")]
    public IActionResult Item([FromQuery] string osItemNo) => Execute(() => Ok(repository.Get(osItemNo)));

    [HttpPost]
    public IActionResult Create([FromBody] Et202WriteRequest request) => Execute(() => Ok(repository.Create(request, Actor)));

    [HttpPut("item")]
    public IActionResult Update([FromQuery] string osItemNo, [FromBody] Et202UpdateRequest request) =>
        Execute(() => Ok(repository.Update(osItemNo, request, Actor)));

    [HttpDelete("item")]
    public IActionResult Delete([FromQuery] string osItemNo, [FromBody] Et202ExpectedRequest request) =>
        Execute(() => Ok(repository.Delete(osItemNo, request, Actor)));

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Et202RuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "外包項目操作失敗", detail: ex.Message); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Problem(statusCode: 409, title: "外包項目識別碼重複", detail: "識別碼已存在；本次未覆寫原始資料。"); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("ET202 database operation failed ({ErrorType}) for {Actor}", ex.GetType().Name, Actor);
            return Problem(statusCode: 503, title: "外包項目服務暫時無法使用",
                detail: "必要結構、權限或引用來源尚未符合契約；請重新查詢確認實際結果。");
        }
    }
}
