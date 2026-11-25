using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Inventory;

[ApiController]
[Authorize]
[Route("api/inventory/fgd-inventory")]
public sealed class InvFgdInventoryController(InvFgdInventoryRepository repository, ILogger<InvFgdInventoryController> logger) : ControllerBase
{
    [HttpGet("lookups/inventories")]
    public IActionResult Inventories([FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        Execute(() => Ok(repository.Inventories(page, pageSize)));

    [HttpGet]
    public IActionResult Stock([FromQuery] string? inventoryNo, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        Execute(() => Ok(repository.Stock(inventoryNo, page, pageSize)));

    [HttpGet("{inventoryNo}/{lotNo}/{locatorNo}/details")]
    public IActionResult StockDetails(string inventoryNo, string lotNo, string locatorNo) =>
        Execute(() => Ok(repository.StockDetails(inventoryNo, lotNo, locatorNo)));

    [HttpGet("confirmations")]
    public IActionResult Confirmations([FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        Execute(() => Ok(repository.Confirmations(page, pageSize)));

    [HttpGet("confirmations/{fgdInNo}")]
    public IActionResult ConfirmationDetails(string fgdInNo) => Execute(() => Ok(repository.ConfirmationDetails(fgdInNo)));

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (InvFgdInventoryRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "成品庫查詢失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("IV501 read failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: 503, title: "成品庫資料服務暫時無法使用",
                detail: "無法讀取成品庫存或待確認入庫單，請確認資料庫結構及應用程式權限已部署。");
        }
    }
}
