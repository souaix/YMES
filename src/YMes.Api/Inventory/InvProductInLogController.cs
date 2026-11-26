using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Inventory;

[ApiController]
[Authorize]
[Route("api/inventory/product-in/logs")]
public sealed class InvProductInLogController(
    InvProductInLogRepository repository,
    ILogger<InvProductInLogController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult List(
        [FromQuery] int? productInType = null,
        [FromQuery] int? state = null,
        [FromQuery] string? productInNo = null,
        [FromQuery] string? inventoryNo = null,
        [FromQuery] string? fromInventoryNo = null,
        [FromQuery] DateOnly? createDateFrom = null,
        [FromQuery] DateOnly? createDateTo = null,
        [FromQuery] DateOnly? inputDateFrom = null,
        [FromQuery] DateOnly? inputDateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50) =>
        Execute(() => Ok(repository.List(productInType, state, productInNo, inventoryNo, fromInventoryNo,
            createDateFrom, createDateTo, inputDateFrom, inputDateTo, page, pageSize)));

    [HttpGet("{productInType:int}/{productInNo}/details")]
    public IActionResult Details(int productInType, string productInNo) =>
        Execute(() => Ok(repository.Details(productInType, productInNo)));

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (InvProductInRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "入庫單記錄查詢失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("IV503 read failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: 503, title: "入庫單記錄資料服務暫時無法使用",
                detail: "無法讀取入庫單主檔或明細，請確認資料庫結構及應用程式權限已部署。");
        }
    }
}
