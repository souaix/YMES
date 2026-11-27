using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/attribute-item-import")]
public sealed class Qc215Controller(NpgsqlDataSource dataSource) : ControllerBase
{
    private const long MultipartLimit = 5L * 1024 * 1024 + 64L * 1024;
    private readonly Qc215Repository _repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpPost("preview")]
    [RequestSizeLimit(MultipartLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = MultipartLimit)]
    public IActionResult Preview([FromForm] IFormFile? file) => Execute(() => Ok(_repository.Preview(file)));

    [HttpPost("check")]
    [RequestSizeLimit(2L * 1024 * 1024)]
    public IActionResult Check([FromBody] Qc215ImportRequest? request) => Execute(() =>
    {
        if (request is null) throw new Qc215RuleException(400, "請提供要檢查的資料列。");
        return Ok(new { rows = _repository.Process(request, Actor, save: false) });
    });

    [HttpPost("import")]
    [RequestSizeLimit(2L * 1024 * 1024)]
    public IActionResult Import([FromBody] Qc215ImportRequest? request) => Execute(() =>
    {
        if (request is null) throw new Qc215RuleException(400, "請提供要儲存的資料列。");
        return Ok(new { rows = _repository.Process(request, Actor, save: true) });
    });

    [HttpGet("template")]
    public IActionResult Template() => Problem(
        statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "QC215 官方範本尚未移轉",
        detail: "YMES 尚未配置 FunctionNo=IMPORT QCITEM ATTRIBUTES 的正式範本檔；請先由 MES 系統範本儲存區移轉 AttributesQCItem_Template.xls/.xlsx。");

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc215RuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "QC215 計數品管項目匯入失敗", detail: ex.Message);
        }
        catch (PostgresException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "QC215 資料操作失敗", detail: "資料庫操作失敗，請確認 YMES 相依表及 ymes_app 權限。");
        }
    }
}
