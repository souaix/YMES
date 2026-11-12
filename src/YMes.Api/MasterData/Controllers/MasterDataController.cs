using System.Security.Claims;
using MES.Api.Data;
using MES.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MES.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class MasterDataController(IMasterDataRepository repository) : ControllerBase
{
    [HttpGet("operations")]
    public ActionResult<IReadOnlyList<OperationDto>> ListOperations([FromQuery] string? keyword, [FromQuery] int? issueState) => Ok(repository.ListOperations(keyword, issueState));

    [HttpGet("operations/{no}")]
    public ActionResult<OperationDto> GetOperation(string no) => Execute(() => repository.GetOperation(no));

    [HttpPost("operations")]
    public ActionResult<OperationDto> CreateOperation([FromBody] OperationWriteRequest request) => Create(() => repository.CreateOperation(request, ActorUserNo), x => $"/api/operations/{Uri.EscapeDataString(x.OperationNo)}");

    [HttpPut("operations/{no}")]
    public ActionResult<OperationDto> UpdateOperation(string no, [FromBody] OperationWriteRequest request) => Execute(() => repository.UpdateOperation(no, request, ActorUserNo), "作業資料無法更新");

    [HttpPost("operations/{no}/approve")]
    public ActionResult<OperationDto> ApproveOperation(string no, [FromBody] MasterDataCommandRequest request) => Execute(() => repository.ApproveOperation(no, request.DataStamp, ActorUserNo), "作業無法核准");

    [HttpDelete("operations/{no}")]
    public IActionResult DeleteOperation(string no, [FromQuery] int issueState, [FromQuery] int dataStamp) => ExecuteAction(() => repository.DeleteOperation(no, issueState, dataStamp, ActorUserNo), "作業無法刪除或停用");

    [HttpGet("materials")]
    public ActionResult<IReadOnlyList<MaterialDto>> ListMaterials([FromQuery] string? keyword, [FromQuery] int? issueState, [FromQuery] string? materialType) => Ok(repository.ListMaterials(keyword, issueState, materialType));

    [HttpGet("materials/{no}")]
    public ActionResult<MaterialDto> GetMaterial(string no) => Execute(() => repository.GetMaterial(no));

    [HttpPost("materials")]
    public ActionResult<MaterialDto> CreateMaterial([FromBody] MaterialWriteRequest request) => Create(() => repository.CreateMaterial(request, ActorUserNo), x => $"/api/materials/{Uri.EscapeDataString(x.MaterialNo)}");

    [HttpPut("materials/{no}")]
    public ActionResult<MaterialDto> UpdateMaterial(string no, [FromBody] MaterialWriteRequest request) => Execute(() => repository.UpdateMaterial(no, request, ActorUserNo), "物料資料無法更新");

    [HttpPost("materials/{no}/approve")]
    public ActionResult<MaterialDto> ApproveMaterial(string no, [FromBody] MasterDataCommandRequest request) => Execute(() => repository.ApproveMaterial(no, request.DataStamp, ActorUserNo), "物料無法核准");

    [HttpDelete("materials/{no}")]
    public IActionResult DeleteMaterial(string no, [FromQuery] int issueState, [FromQuery] int dataStamp) => ExecuteAction(() => repository.DeleteMaterial(no, issueState, dataStamp, ActorUserNo), "物料無法刪除或停用");

    [HttpGet("processes")]
    public ActionResult<IReadOnlyList<ProcessDto>> ListProcesses([FromQuery] string? keyword, [FromQuery] int? issueState) => Ok(repository.ListProcesses(keyword, issueState));

    [HttpGet("processes/{no}/{version}")]
    public ActionResult<ProcessDto> GetProcess(string no, string version) => Execute(() => repository.GetProcess(no, version));

    [HttpPost("processes")]
    public ActionResult<ProcessDto> CreateProcess([FromBody] ProcessWriteRequest request) => Create(() => repository.CreateProcess(request, ActorUserNo), x => $"/api/processes/{Uri.EscapeDataString(x.ProcessNo)}/{Uri.EscapeDataString(x.ProcessVersion)}");

    [HttpPut("processes/{no}/{version}")]
    public ActionResult<ProcessDto> UpdateProcess(string no, string version, [FromBody] ProcessWriteRequest request) => Execute(() => repository.UpdateProcess(no, version, request, ActorUserNo), "流程資料無法更新");

    [HttpPost("processes/{no}/{version}/approve")]
    public ActionResult<ProcessDto> ApproveProcess(string no, string version, [FromBody] MasterDataCommandRequest request) => Execute(() => repository.ApproveProcess(no, version, request.DataStamp, ActorUserNo), "流程無法核准");

    [HttpPost("processes/{no}/{version}/new-version")]
    public ActionResult<ProcessDto> NewProcessVersion(string no, string version) => Execute(() => repository.CreateProcessVersion(no, version, ActorUserNo), "無法產生新流程版本");

    [HttpPost("processes/{no}/{version}/set-current")]
    public ActionResult<ProcessDto> SetCurrentProcess(string no, string version, [FromBody] MasterDataCommandRequest request) => Execute(() => repository.SetCurrentProcess(no, version, request.DataStamp, ActorUserNo), "無法設定目前流程版本");

    [HttpGet("processes/{no}/{version}/graph")]
    public ActionResult<ProcessGraphDto> GetGraph(string no, string version) => Execute(() => repository.GetGraph(no, version));

    [HttpPut("processes/{no}/{version}/graph")]
    public ActionResult<ProcessGraphDto> UpdateGraph(string no, string version, [FromBody] ProcessGraphDto request) => Execute(() => repository.UpdateGraph(no, version, request, ActorUserNo), "流程圖無法儲存");

    [HttpDelete("processes/{no}/{version}")]
    public IActionResult DeleteProcess(string no, string version, [FromQuery] int issueState, [FromQuery] int dataStamp) => ExecuteAction(() => repository.DeleteProcess(no, version, issueState, dataStamp, ActorUserNo), "流程無法刪除或停用");

    [HttpGet("products")]
    public ActionResult<IReadOnlyList<ProductDto>> ListProducts([FromQuery] string? keyword, [FromQuery] int? issueState) => Ok(repository.ListProducts(keyword, issueState));

    [HttpGet("products/{no}/versions/{version}")]
    public ActionResult<ProductDto> GetProduct(string no, string version) => Execute(() => repository.GetProduct(no, version));

    [HttpPost("products")]
    public ActionResult<ProductDto> CreateProduct([FromBody] ProductWriteRequest request) => Create(() => repository.CreateProduct(request, ActorUserNo), x => $"/api/products/{Uri.EscapeDataString(x.ProductNo)}/versions/{Uri.EscapeDataString(x.ProductVersion)}");

    [HttpPut("products/{no}/versions/{version}")]
    public ActionResult<ProductDto> UpdateProduct(string no, string version, [FromBody] ProductWriteRequest request) => Execute(() => repository.UpdateProduct(no, version, request, ActorUserNo), "產品資料無法更新");

    [HttpPost("products/{no}/versions/{version}/approve")]
    public ActionResult<ProductDto> ApproveProduct(string no, string version, [FromBody] MasterDataCommandRequest request) => Execute(() => repository.ApproveProduct(no, version, request.DataStamp, ActorUserNo), "產品無法核准");

    [HttpPost("products/{no}/versions/{version}/set-current")]
    public ActionResult<ProductDto> SetCurrentProduct(string no, string version, [FromBody] MasterDataCommandRequest request) => Execute(() => repository.SetCurrentProduct(no, version, request.DataStamp, ActorUserNo), "無法設定目前產品版本");

    [HttpPost("products/{no}/versions/{version}/new-version")]
    public ActionResult<ProductDto> NewProductVersion(string no, string version) => Execute(() => repository.CreateProductVersion(no, version, ActorUserNo), "無法產生新產品版本");

    [HttpDelete("products/{no}/versions/{version}")]
    public IActionResult DeleteProduct(string no, string version, [FromQuery] int issueState, [FromQuery] int dataStamp) => ExecuteAction(() => repository.DeleteProduct(no, version, issueState, dataStamp, ActorUserNo), "產品無法刪除或停用");

    [HttpGet("products/{no}/versions/{version}/processes")]
    public ActionResult<IReadOnlyList<ProductProcessDto>> GetProductProcesses(string no, string version) => Execute(() => repository.GetProductProcesses(no, version));

    [HttpPost("products/{no}/versions/{version}/processes")]
    public ActionResult<ProductProcessDto> AddProductProcess(string no, string version, [FromBody] ProductProcessWriteRequest request) => Create(() => repository.AddProductProcess(no, version, request, ActorUserNo));

    [HttpPut("products/{no}/versions/{version}/processes/{processNo}/{selector}")]
    public ActionResult<ProductProcessDto> UpdateProductProcess(string no, string version, string processNo, string selector, [FromBody] ProductProcessWriteRequest request) =>
        Execute(() => repository.UpdateProductProcess(no, version, processNo, selector, request, ActorUserNo), "產品流程無法更新");

    [HttpDelete("products/{no}/versions/{version}/processes/{processNo}/{selector}")]
    public IActionResult DeleteProductProcess(string no, string version, string processNo, string selector, [FromQuery] int moTypeNo, [FromQuery] string productionSegmentNo) =>
        ExecuteAction(() => repository.DeleteProductProcess(no, version, moTypeNo, productionSegmentNo, processNo, selector, ActorUserNo), "產品流程無法刪除");

    [HttpGet("products/{no}/versions/{version}/bom")]
    public ActionResult<IReadOnlyList<ProductBomDto>> GetBom(string no, string version) => Execute(() => repository.GetBom(no, version));

    [HttpPost("products/{no}/versions/{version}/bom")]
    public ActionResult<ProductBomDto> AddBom(string no, string version, [FromBody] ProductBomWriteRequest request) => Create(() => repository.AddBom(no, version, request, ActorUserNo));

    [HttpDelete("products/{no}/versions/{version}/bom/{id}")]
    public IActionResult DeleteBom(string no, string version, string id) => ExecuteAction(() => repository.DeleteBom(no, version, id, ActorUserNo), "BOM 無法刪除");

    private string ActorUserNo => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "ymes";

    private ActionResult<T> Execute<T>(Func<T> action, string title = "主檔讀取失敗")
    {
        try { return Ok(action()); }
        catch (MasterDataRuleException ex) { return Problem(statusCode: ex.StatusCode, title: title, detail: ex.Message); }
    }

    private ActionResult<T> Create<T>(Func<T> action, Func<T, string>? location = null)
    {
        try
        {
            var item = action();
            return location is null ? Ok(item) : Created(location(item), item);
        }
        catch (MasterDataRuleException ex) { return Problem(statusCode: ex.StatusCode, title: "主檔資料無法建立", detail: ex.Message); }
    }

    private IActionResult ExecuteAction(Action action, string title)
    {
        try { action(); return NoContent(); }
        catch (MasterDataRuleException ex) { return Problem(statusCode: ex.StatusCode, title: title, detail: ex.Message); }
    }
}

public sealed record MasterDataCommandRequest(int DataStamp);
