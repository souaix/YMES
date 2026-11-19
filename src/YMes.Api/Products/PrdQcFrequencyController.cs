using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Products;

[ApiController]
[Authorize]
[Route("api/prd/qc-frequency-rules")]
public sealed class PrdQcFrequencyController(NpgsqlDataSource dataSource, ILogger<PrdQcFrequencyController> logger) : ControllerBase
{
    private readonly PrdQcFrequencyRepository repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet]
    public IActionResult List([FromQuery] string? search = null) => Execute(() => Ok(repository.List(search)));
    [HttpGet("item")]
    public IActionResult Get([FromQuery] string? productNo, [FromQuery] string? equipmentNo, [FromQuery] string? opNo) =>
        Execute(() => Ok(repository.Get(new() { ProductNo = productNo, EquipmentNo = equipmentNo, OpNo = opNo })));
    [HttpGet("options/products")]
    public IActionResult Products() => Execute(() => Ok(repository.Options("products")));
    [HttpGet("options/equipment")]
    public IActionResult Equipment() => Execute(() => Ok(repository.Options("equipment")));
    [HttpGet("options/operations")]
    public IActionResult Operations() => Execute(() => Ok(repository.Options("operations")));
    [HttpPost]
    public IActionResult Create([FromBody] PrdQcFrequencyWriteRequest request) => Write("Create", () => repository.Create(request, Actor));
    [HttpPut]
    public IActionResult Update([FromBody] PrdQcFrequencyUpdateRequest request) => Write("Update", () => repository.Update(request, Actor));
    [HttpDelete]
    public IActionResult Delete([FromBody] PrdQcFrequencyDeleteRequest request) => Write("Delete", () => repository.Delete(request, Actor));

    private IActionResult Write(string action, Func<PrdQcFrequencyDto> operation) => Execute(() =>
    {
        var result = operation();
        logger.LogInformation("PD206 {Action} completed for {ProductNo}/{EquipmentNo}/{OpNo} by {Actor}; stamp {Stamp}",
            action, result.ProductNo, result.EquipmentNo, result.OpNo, Actor, result.DataStamp);
        return Ok(result);
    });
    private IActionResult Execute(Func<IActionResult> operation)
    {
        try { return operation(); }
        catch (PrdQcFrequencyRuleException ex) { return Problem(statusCode: ex.StatusCode, title: "抽樣頻率操作失敗", detail: ex.Message); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Problem(statusCode: 409, title: "抽樣頻率衝突", detail: "這組產品、設備與作業站已存在規則，請重新查詢。"); }
        catch (NpgsqlException ex)
        {
            logger.LogError("PD206 database operation failed for {Actor}; error {ErrorType}", Actor, ex.GetType().Name);
            return Problem(statusCode: 503, title: "抽樣頻率服務暫時無法使用",
                detail: "YMES 抽樣頻率資料庫操作失敗，請確認主檔、產品／設備／作業站相依表及應用權限已部署。");
        }
    }
}
