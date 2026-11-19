using System.Security.Claims;
using MES.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Products;

[ApiController]
[Authorize]
[Route("api/products/{no}/versions/{version}/recipe-groups")]
public sealed class PrdRecipeGroupController(NpgsqlDataSource dataSource, ILogger<PrdRecipeGroupController> logger) : ControllerBase
{
    private readonly PrdRecipeGroupRepository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List(string no, string version) => Execute(() => Ok(repository.List(no, version)));

    [HttpGet("options")]
    public IActionResult Options(string no, string version) => Execute(() => Ok(repository.Options(no, version)));

    [HttpPost]
    public IActionResult Create(string no, string version, [FromBody] PrdRecipeGroupWriteRequest request) =>
        Write("Add", () => repository.Create(no, version, request, Actor));

    [HttpDelete("{recipeGroup}")]
    public IActionResult Delete(string no, string version, string recipeGroup) =>
        Write("Delete", () => repository.Delete(no, version, recipeGroup, Actor));

    private IActionResult Write(string operation, Func<PrdRecipeGroupAssignmentDto> action) => Execute(() =>
    {
        var result = action();
        logger.LogInformation("PRD RecipeGroup {Operation} for {ProductNo}/{ProductVersion}, {RecipeGroup}, by {Actor}",
            operation, result.ProductNo, result.ProductVersion, result.RecipeGroup, Actor);
        return Ok(result);
    });

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (MasterDataRuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "產品 RecipeGroup 操作失敗", detail: ex.Message);
        }
        catch (NpgsqlException ex)
        {
            logger.LogError("PRD RecipeGroup database operation failed for {Actor}; error {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "產品 RecipeGroup 資料操作失敗",
                detail: "YMES 資料庫操作失敗，請確認產品 RecipeGroup 資料表與必要權限已部署。");
        }
    }
}
