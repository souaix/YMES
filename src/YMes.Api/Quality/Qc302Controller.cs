using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/chart-data-import")]
public sealed class Qc302Controller(NpgsqlDataSource dataSource) : ControllerBase
{
    private const long MultipartLimit = 22L * 1024 * 1024;
    private readonly Qc302Repository _repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpPost("preview")]
    [RequestSizeLimit(MultipartLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = MultipartLimit)]
    public IActionResult Preview([FromForm] IFormFile? file) => Execute(() => Ok(_repository.Preview(file)));

    [HttpPost("import")]
    [RequestSizeLimit(5L * 1024 * 1024)]
    public IActionResult Import([FromBody] Qc302ImportRequest? request) => Execute(() =>
    {
        if (request is null) throw new Qc302RuleException(StatusCodes.Status400BadRequest, "請提供匯入列。 ");
        return Ok(new { rows = _repository.Import(request, Actor) });
    });

    [HttpGet("template")]
    public IActionResult Template()
    {
        // A production FunctionNo template binary has not been migrated into YMES.
        // Do not return a generated DEV workbook as the official MES template.
        return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "QC302 官方範本尚未移轉",
            detail: "YMES 尚未配置 FunctionNo=IMPORT QC CHART DATA 的正式範本檔；請先由 MES 系統範本儲存區移轉 QCChart_Template.xls/.xlsx。");
    }

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc302RuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "QC302 管制圖匯入失敗", detail: ex.Message);
        }
        catch (PostgresException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "QC302 資料操作失敗", detail: "資料庫操作失敗，請確認 YMES schema 與相依表已部署。");
        }
    }
}
