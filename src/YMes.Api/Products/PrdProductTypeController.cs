using System.Security.Claims;
using MES.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Products;

[ApiController]
[Authorize]
[Route("api/prd/product-types")]
public sealed class PrdProductTypeController(NpgsqlDataSource dataSource, ILogger<PrdProductTypeController> logger) : ControllerBase
{
    private readonly PrdProductTypeRepository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? search, [FromQuery] int? issueState) => Execute(() => Ok(repository.List(search, issueState)));

    [HttpGet("item")]
    public IActionResult Get([FromQuery] string productType) => Execute(() => Ok(repository.Get(productType)));

    [HttpPost]
    public IActionResult Create([FromBody] PrdProductTypeWriteRequest request) => Execute(() => Written("created", repository.Create(request, Actor)));

    [HttpPut("item")]
    public IActionResult Update([FromQuery] string productType, [FromBody] PrdProductTypeUpdateRequest request) =>
        Execute(() => Written("updated", repository.Update(productType, request, Actor)));

    [HttpPost("approve")]
    public IActionResult Approve([FromQuery] string productType, [FromBody] PrdProductTypeCommandRequest request) =>
        Execute(() => Written("approved", repository.Approve(productType, request, Actor)));

    [HttpDelete("item")]
    public IActionResult Delete([FromQuery] string productType, [FromBody] PrdProductTypeCommandRequest request) => Execute(() =>
    {
        var result = repository.Delete(productType, request, Actor);
        logger.LogInformation("PD101 {Action} {ProductType} by {Actor}", result.Outcome, result.Item.ProductType, Actor);
        return Ok(result);
    });

    [HttpPost("copy")]
    public IActionResult Copy() => Problem(statusCode: 503, title: "產品類別複製尚未開放",
        detail: "完整類別複製包含流程、屬性、參數及其他設定，目前尚未接入；沒有建立副本。");

    private IActionResult Written(string action, PrdProductTypeDto item)
    {
        logger.LogInformation("PD101 {Action} {ProductType} by {Actor}; state {IssueState}, stamp {DataStamp}",
            action, item.ProductType, Actor, item.IssueState, item.DataStamp);
        return Ok(item);
    }

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (MasterDataRuleException ex) { return Problem(statusCode: ex.StatusCode, title: "產品類別操作失敗", detail: ex.Message); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Problem(statusCode: 409, title: "產品類別已存在", detail: "產品類別編號已存在，請重新查詢。"); }
        catch (NpgsqlException ex)
        {
            logger.LogError("PD101 database operation failed for {Actor}: {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: 503, title: "產品類別服務暫時無法使用",
                detail: "無法存取產品類別或參照設定，請稍後重新查詢確認資料。");
        }
    }
}
