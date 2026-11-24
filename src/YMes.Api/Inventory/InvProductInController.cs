using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Inventory;

[ApiController]
[Authorize]
[Route("api/inventory/product-in")]
public sealed class InvProductInController(InvProductInRepository repository, ILogger<InvProductInController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult List(
        [FromQuery] int? productInType = 0,
        [FromQuery] int? state = 0,
        [FromQuery] int? source = 1,
        [FromQuery] string? inventoryNo = null,
        [FromQuery] string? fromInventoryNo = null,
        [FromQuery] string? productInNo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50) =>
        Execute(() => Ok(repository.List(productInType, state, source, inventoryNo, fromInventoryNo, productInNo, page, pageSize)));

    [HttpGet("{productInType:int}/{productInNo}/details")]
    public IActionResult Details(int productInType, string productInNo) =>
        Execute(() => Ok(repository.Details(productInType, productInNo)));

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (InvProductInRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "入庫單查詢失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("IV502 read failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: 503, title: "入庫單資料服務暫時無法使用",
                detail: "無法讀取入庫單主檔或明細，請確認資料庫結構及應用程式權限已部署。");
        }
    }
}
