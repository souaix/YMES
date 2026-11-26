using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Reports;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class OpStateBoardController(
    OpStateBoardRepository repository,
    ILogger<OpStateBoardController> logger) : ControllerBase
{
    [HttpGet("op-state")]
    public async Task<IActionResult> GetBoard(
        [FromQuery] string? opNos,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await repository.GetBoardAsync(opNos, cancellationToken));
        }
        catch (OpStateRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "作業站現況查詢失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("RT201 board query failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: 503, title: "作業站現況資料服務暫時無法使用",
                detail: "無法讀取作業站或在製批次資料，請確認資料表結構與應用程式讀取權限。");
        }
    }

    [HttpGet("op-state-breakdown")]
    public async Task<IActionResult> GetBreakdown(
        [FromQuery] string? opNos,
        [FromQuery] string? groupBy,
        [FromQuery] string? productType,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await repository.GetBreakdownAsync(opNos, groupBy, productType, cancellationToken));
        }
        catch (OpStateRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "作業站產品分布查詢失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("RT201 breakdown query failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: 503, title: "作業站產品分布資料服務暫時無法使用",
                detail: "無法讀取作業站、在製批次或產品資料，請確認資料表結構與應用程式讀取權限。");
        }
    }

    [HttpGet("op-state-lots")]
    public async Task<IActionResult> GetLots(
        [FromQuery] string? opNos,
        [FromQuery] string? statuses,
        [FromQuery] string? productType,
        [FromQuery] string? productNo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await repository.GetLotsAsync(opNos, statuses, productType, productNo,
                page, pageSize, cancellationToken));
        }
        catch (OpStateRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "作業站批次明細查詢失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("RT201 lot detail query failed ({ErrorType})", ex.GetType().Name);
            return Problem(statusCode: 503, title: "作業站批次明細資料服務暫時無法使用",
                detail: "無法讀取在製批次或產品資料，請確認資料表結構與應用程式讀取權限。");
        }
    }
}
