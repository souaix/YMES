using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/inspections")]
public sealed class Qc217Controller(NpgsqlDataSource dataSource) : ControllerBase
{
    private readonly Qc217Repository _repository = new(dataSource);
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    [HttpGet]
    public IActionResult List(
        [FromQuery] int issueState = 0,
        [FromQuery] string? opNo = null,
        [FromQuery] string? equipmentNo = null,
        [FromQuery] string? inspType = null) => Execute(() => Ok(new
        {
            items = _repository.List(issueState, opNo, equipmentNo, inspType)
        }));

    [HttpGet("options")]
    public IActionResult Options() => Execute(() => Ok(_repository.GetOptions()));

    [HttpGet("{opNo}/{equipmentNo}/{inspType:int}/details")]
    public IActionResult Details(string opNo, string equipmentNo, int inspType) => Execute(() => Ok(new
    {
        items = _repository.GetDetails(opNo, equipmentNo, inspType)
    }));

    [HttpPost]
    public IActionResult Create([FromBody] Qc217InspectionWriteRequest request) => Execute(() => Ok(_repository.Create(request, Actor)));

    [HttpPut("{opNo}/{equipmentNo}/{inspType:int}/{inspWeek:int}/{inspStartTime}")]
    public IActionResult Update(string opNo, string equipmentNo, int inspType, int inspWeek, string inspStartTime,
        [FromBody] Qc217InspectionWriteRequest request) => Execute(() => Ok(_repository.Update(
            new Qc217MainKey(opNo, equipmentNo, inspType, inspWeek, inspStartTime), request, Actor)));

    [HttpDelete("{opNo}/{equipmentNo}/{inspType:int}/{inspWeek:int}/{inspStartTime}")]
    public IActionResult Delete(string opNo, string equipmentNo, int inspType, int inspWeek, string inspStartTime) => Execute(() =>
    {
        if (!Request.Headers.TryGetValue("If-Match", out var value) || value.Count != 1)
            throw new Qc217RuleException(StatusCodes.Status428PreconditionRequired, "刪除設備品檢設定時必須提供目前的 If-Match DataStamp。");
        var stampText = value[0]?.Trim().Trim('"');
        if (!decimal.TryParse(stampText, NumberStyles.Number, CultureInfo.InvariantCulture, out var stamp) || stamp < 0)
            throw new Qc217RuleException(StatusCodes.Status400BadRequest, "If-Match 必須是有效的 DataStamp。");
        return Ok(_repository.Delete(new Qc217MainKey(opNo, equipmentNo, inspType, inspWeek, inspStartTime), stamp, Actor));
    });

    [HttpPost("{opNo}/{equipmentNo}/{inspType:int}/{inspWeek:int}/{inspStartTime}/approval")]
    public IActionResult Approve(string opNo, string equipmentNo, int inspType, int inspWeek, string inspStartTime,
        [FromBody] Qc217StampRequest request) => Execute(() => Ok(_repository.Approve(
            new Qc217MainKey(opNo, equipmentNo, inspType, inspWeek, inspStartTime), request.DataStamp, Actor)));

    private IActionResult Execute(Func<IActionResult> action)
    {
        try { return action(); }
        catch (Qc217RuleException ex)
        {
            return Problem(statusCode: ex.StatusCode, title: "QC217 設備品檢資料操作失敗", detail: ex.Message);
        }
    }
}
