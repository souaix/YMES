using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/spc/insp-machine-setup")]
public sealed class Qc501SpcInspMachineSetupController(
    NpgsqlDataSource dataSource,
    ILogger<Qc501SpcInspMachineSetupController> logger) : ControllerBase
{
    private readonly Qc501SpcInspMachineSetupRepository repository = new(dataSource);

    [HttpGet("lookups")]
    public IActionResult Lookups() => Execute(() => Ok(repository.Lookups()));

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (QcInspectionRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "SC501 選項載入失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogError(ex, "SC501 inspection machine setup lookup failed");
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "SC501 選項載入失敗", detail: "YMES 查詢資料失敗，請確認 SC501 查詢資料表及權限已部署。");
        }
    }
}
