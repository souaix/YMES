using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Enterprise;

public abstract class Et106ControllerBase(ILogger logger) : ControllerBase
{
    protected string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";
    protected string RawQuery(string name)
    {
        // Query values are already URL decoded. Preserve empty/raw whitespace;
        // the standard string binder can otherwise turn legitimate raw keys NULL.
        if (!Request.Query.TryGetValue(name, out var values) || values.Count != 1 || values[0] is null)
            throw new Et106RuleException(400, name + " 必須明確提供一個原始值。");
        return values[0]!;
    }
    protected IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Et106RuleException error)
        { return Problem(statusCode: error.StatusCode, title: "出貨地址／報關操作失敗", detail: error.Message); }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Problem(statusCode: 409, title: "來源鍵值重複", detail: "目的出貨地址鍵已存在，原資料未變更。"); }
        catch (NpgsqlException error)
        {
            logger.LogWarning("ET106 database operation failed ({ErrorType}) for {Actor}", error.GetType().Name, Actor);
            return Problem(statusCode: 503, title: "出貨地址／報關服務暫時無法使用",
                detail: "無法存取必要來源，請稍後重新查詢。");
        }
    }
}

[ApiController]
[Authorize]
[Route("api/ent/customer-ship-addresses")]
public sealed class Et106AddressController(Et106Repository repository, ILogger<Et106AddressController> logger)
    : Et106ControllerBase(logger)
{
    [HttpGet("lookups/customers")]
    public IActionResult Customers() => Execute(() => Ok(new { items = repository.Customers() }));
    [HttpGet]
    public IActionResult List() => Execute(() => Ok(new { items = repository.ListAddresses(RawQuery("customerNo")) }));
    [HttpGet("item")]
    public IActionResult Item() => Execute(() => Ok(repository.GetAddress(RawQuery("customerNo"), RawQuery("shipAddr"))));
    [HttpPost]
    public IActionResult Create([FromBody] Et106AddressWriteRequest request) =>
        Execute(() => Ok(repository.CreateAddress(RawQuery("customerNo"), request, Actor)));
    [HttpPut("item")]
    public IActionResult Update([FromBody] Et106AddressUpdateRequest request) =>
        Execute(() => Ok(repository.UpdateAddress(RawQuery("customerNo"), RawQuery("shipAddr"), request, Actor)));
    [HttpDelete("item")]
    public IActionResult Delete([FromBody] Et106AddressExpectedRequest request) => Execute(() =>
    { repository.DeleteAddress(RawQuery("customerNo"), RawQuery("shipAddr"), request, Actor); return NoContent(); });
}

[ApiController]
[Authorize]
[Route("api/ent/customer-ship-declarations")]
public sealed class Et106DeclarationController(Et106Repository repository, ILogger<Et106DeclarationController> logger)
    : Et106ControllerBase(logger)
{
    [HttpGet]
    public IActionResult List() => Execute(() => Ok(repository.ListDeclarations(RawQuery("customerNo"))));
    [HttpPost]
    public IActionResult Create([FromBody] Et106DeclarationWriteRequest request) =>
        Execute(() => Ok(repository.CreateDeclaration(RawQuery("customerNo"), request, Actor)));
    [HttpPut("group")]
    public IActionResult Update([FromBody] Et106DeclarationUpdateRequest request) =>
        Execute(() => Ok(repository.UpdateDeclarationGroup(RawQuery("customerNo"), request, Actor)));
    [HttpDelete("group")]
    public IActionResult Delete([FromBody] Et106DeclarationExpectedRequest request) =>
        Execute(() => Ok(repository.DeleteDeclarationGroup(RawQuery("customerNo"), request, Actor)));
}
