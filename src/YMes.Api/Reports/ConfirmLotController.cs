using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Reports;

[ApiController]
[Authorize]
[Route("api/reports/confirm-lots")]
public sealed class ConfirmLotController(
    ConfirmLotRepository repository,
    ILogger<ConfirmLotController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? roNo = null,
        [FromQuery] string? moNo = null,
        [FromQuery] string? productNo = null,
        [FromQuery] string? productVersion = null,
        [FromQuery] string? customerNo = null,
        [FromQuery] string? lotNo = null,
        [FromQuery] string? opNo = null,
        [FromQuery] string? orgLotNo = null,
        [FromQuery] string? psNo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await repository.ListAsync(roNo, moNo, productNo, productVersion,
                customerNo, lotNo, opNo, orgLotNo, psNo, page, pageSize, cancellationToken));
        }
        catch (ConfirmLotRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "生產批狀態查詢失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("RT501 query failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: 503, title: "生產批狀態資料服務暫時無法使用",
                detail: "無法讀取生產批或庫存資料，請確認資料表結構及應用程式讀取權限已部署。");
        }
    }
}
