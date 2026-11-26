using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Quality;

[ApiController]
[Authorize]
[Route("api/qc/variable-qc-items")]
public sealed class Qc212VariableQcItemLookupController(NpgsqlDataSource dataSource) : ControllerBase
{
    private readonly Qc212Repository repository = new(dataSource);

    [HttpGet]
    public IActionResult List([FromQuery] int? issueState) => Execute(() => Ok(repository.VariableQcItems(issueState)));

    private IActionResult Execute(Func<IActionResult> operation)
    {
        try { return operation(); }
        catch (Qc212RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "計量品管項目查詢失敗", detail: ex.Message); }
        catch (NpgsqlException) { return Problem(statusCode: 503, title: "計量品管項目查詢失敗", detail: "資料庫操作失敗，請確認品管項目主檔與 SELECT 權限已部署。"); }
    }
}

[ApiController]
[Authorize]
[Route("api/process/operations")]
public sealed class Qc212OperationLookupController(NpgsqlDataSource dataSource) : ControllerBase
{
    private readonly Qc212Repository repository = new(dataSource);

    [HttpGet]
    public IActionResult List() => Execute(() => Ok(repository.Operations()));

    private IActionResult Execute(Func<IActionResult> operation)
    {
        try { return operation(); }
        catch (Qc212RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "作業站查詢失敗", detail: ex.Message); }
        catch (NpgsqlException) { return Problem(statusCode: 503, title: "作業站查詢失敗", detail: "資料庫操作失敗，請確認作業站主檔與 SELECT 權限已部署。"); }
    }
}

[ApiController]
[Authorize]
[Route("api/system/mes-parameters")]
public sealed class Qc212MesParameterLookupController(NpgsqlDataSource dataSource) : ControllerBase
{
    private readonly Qc212Repository repository = new(dataSource);

    [HttpGet]
    public IActionResult List([FromQuery] int? issueState, [FromQuery] int? parameterClass) =>
        Execute(() => Ok(repository.MesParameters(issueState, parameterClass)));

    private IActionResult Execute(Func<IActionResult> operation)
    {
        try { return operation(); }
        catch (Qc212RuleException ex) { return Problem(statusCode: ex.StatusCode, title: "MES Parameter 查詢失敗", detail: ex.Message); }
        catch (NpgsqlException) { return Problem(statusCode: 503, title: "MES Parameter 查詢失敗", detail: "資料庫操作失敗，請確認系統參數主檔與 SELECT 權限已部署。"); }
    }
}
