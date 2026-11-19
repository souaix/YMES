using System.Security.Claims;
using MES.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Products;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class PrdProgramController(NpgsqlDataSource dataSource, ILogger<PrdProgramController> logger) : ControllerBase
{
    private readonly PrdProgramRepository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet("options/programs")]
    public IActionResult Options() => Execute(() => Ok(repository.Options()));

    [HttpGet("{productNo}/versions/{version}/programs")]
    public IActionResult Get(string productNo, string version) => Execute(() => Ok(repository.Get(productNo, version)));

    [HttpPut("{productNo}/versions/{version}/programs")]
    public IActionResult Replace(string productNo, string version, [FromBody] PrdProgramWriteRequest request) => Execute(() =>
    {
        var result = repository.Replace(productNo, version, request, Actor);
        logger.LogInformation("PD102 Program assignment replaced for {ProductNo}/{ProductVersion} by {Actor}; selected {Count}",
            result.ProductNo, result.ProductVersion, Actor, result.ProgramNos.Count);
        return Ok(result);
    });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (MasterDataRuleException ex) { return Problem(statusCode: ex.StatusCode, title: "產品程式指派操作失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogError("PD102 Program database operation failed for {Actor}; error {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: 503, title: "產品程式指派服務暫時無法使用",
                detail: "YMES 產品程式資料庫操作失敗，請確認產品主檔、Program 主檔、指派表與應用權限已部署。");
        }
    }
}
