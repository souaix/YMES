using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Products;

[ApiController]
[Authorize]
[Route("api/products/options")]
public sealed class PrdProductOptionsController(NpgsqlDataSource dataSource, ILogger<PrdProductOptionsController> logger) : ControllerBase
{
    [HttpGet("product-types")]
    public IActionResult ProductTypes() => Read(() =>
    {
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("SELECT producttype,description FROM \"YMES\".tblprdtype WHERE issuestate=2 ORDER BY producttype;", connection);
        using var reader = command.ExecuteReader();
        var items = new List<ProductTypeOption>();
        while (reader.Read()) items.Add(new(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
        return Ok(new { items });
    });

    [HttpGet("units")]
    public IActionResult Units() => Read(() =>
    {
        // YMES explicitly derives selectable unit types from its approved unit
        // catalog. An empty catalog stays empty; there is no DEV fallback.
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("SELECT unitno,unittype,description FROM \"YMES\".tblsysunitbasis WHERE issuestate=2 ORDER BY unittype,unitno;", connection);
        using var reader = command.ExecuteReader();
        var items = new List<UnitOption>();
        while (reader.Read()) items.Add(new(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)));
        return Ok(new { items });
    });

    private IActionResult Read(Func<IActionResult> action)
    {
        try { return action(); }
        catch (NpgsqlException exception)
        {
            logger.LogError("Product entry catalog read failed: {ErrorType}", exception.GetType().Name);
            return Problem(statusCode: 503, title: "產品選項暫時無法載入", detail: "請確認產品類別、單位目錄及應用程式讀取權限已備妥。");
        }
    }

    public sealed record ProductTypeOption(string ProductType, string? Description);
    public sealed record UnitOption(string UnitNo, string UnitType, string? Description);
}
