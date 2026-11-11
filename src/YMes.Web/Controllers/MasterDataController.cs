using System.Text.Json;
using System.Text.Encodings.Web;
using MES.Contracts;
using MES.Web.Models;
using MES.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MES.Web.Controllers;

public sealed class MasterDataController(MasterDataApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Operations(string? keyword, int? issueState, string? selected, CancellationToken ct)
    {
        try
        {
            issueState ??= 0;
            var path = Query("api/operations", keyword, issueState);
            var items = await api.GetListAsync<OperationDto>(path, ct);
            return View(new OperationIndexViewModel { Items = items, Selected = items.FirstOrDefault(x => Eq(x.OperationNo, selected)) ?? items.FirstOrDefault(), Keyword = keyword, IssueState = issueState });
        }
        catch (HttpRequestException) { return View("ApiUnavailable"); }
    }

    [HttpGet]
    public async Task<IActionResult> NewOperation(CancellationToken ct) { await LoadOperationLookups(ct); return View("OperationForm", new OperationWriteRequest { OperationClass = 1, CountOperationUnitQty = 1, ProductionSegmentNo = "FRONT" }); }

    [HttpGet]
    public async Task<IActionResult> CopyOperation(string id, CancellationToken ct)
    {
        var item = await api.GetAsync<OperationDto>($"api/operations/{E(id)}", ct);
        if (item is null) return NotFound();
        await LoadOperationLookups(ct);
        var request = ToRequest(item); request.OperationNo = ""; request.DataStamp = 0;
        ViewData["IsCopy"] = true;
        return View("OperationForm", request);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NewOperation(OperationWriteRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadOperationLookups(ct); return View("OperationForm", request); }
        try { await api.PostAsync<OperationWriteRequest, OperationDto>("api/operations", request, ct); TempData["Notice"] = $"作業 {request.OperationNo} 已建立。"; return RedirectToAction(nameof(Operations), new { selected = request.OperationNo }); }
        catch (HttpRequestException ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadOperationLookups(ct); return View("OperationForm", request); }
    }

    [HttpGet]
    public async Task<IActionResult> EditOperation(string id, CancellationToken ct)
    {
        var item = await api.GetAsync<OperationDto>($"api/operations/{E(id)}", ct);
        if (item is null) return NotFound();
        if (item.IssueState != 0) return Conflict("只有 UNFROZEN 作業可以編輯。");
        await LoadOperationLookups(ct);
        ViewData["CreatedAt"] = item.CreatedAt?.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss") ?? "";
        ViewData["RevisedAt"] = "";
        return View("OperationForm", ToRequest(item));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditOperation(string id, OperationWriteRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadOperationLookups(ct); return View("OperationForm", request); }
        try { await api.PutAsync<OperationWriteRequest, OperationDto>($"api/operations/{E(id)}", request, ct); TempData["Notice"] = $"作業 {id} 已更新。"; return RedirectToAction(nameof(Operations), new { selected = id }); }
        catch (HttpRequestException ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadOperationLookups(ct); return View("OperationForm", request); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> ApproveOperation(string no, int stamp, CancellationToken ct) => Approve("operations", no, stamp, nameof(Operations), ct);
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteOperation(string no, int state, int stamp, CancellationToken ct) => Remove("operations", no, null, state, stamp, nameof(Operations), ct);

    [HttpGet]
    public async Task<IActionResult> Materials(string? keyword, int? issueState, string? selected, CancellationToken ct)
    {
        try
        {
            issueState ??= 0;
            var items = await api.GetListAsync<MaterialDto>(Query("api/materials", keyword, issueState), ct);
            return View(new MaterialIndexViewModel { Items = items, Selected = items.FirstOrDefault(x => Eq(x.MaterialNo, selected)) ?? items.FirstOrDefault(), Keyword = keyword, IssueState = issueState });
        }
        catch (HttpRequestException) { return View("ApiUnavailable"); }
    }

    [HttpGet]
    public async Task<IActionResult> NewMaterial(CancellationToken ct) { await LoadMaterialLookups(ct); return View("MaterialForm", new MaterialWriteRequest { MaterialType = "MATERIAL", PutInPlace = "OP", UnitType = "PCS", UnitNo = "PCS", CheckLotNo = 1 }); }

    [HttpGet]
    public async Task<IActionResult> CopyMaterial(string id, CancellationToken ct)
    {
        var item = await api.GetAsync<MaterialDto>($"api/materials/{E(id)}", ct);
        if (item is null) return NotFound();
        await LoadMaterialLookups(ct);
        var request = ToRequest(item); request.MaterialNo = ""; request.DataStamp = 0;
        ViewData["IsCopy"] = true;
        return View("MaterialForm", request);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NewMaterial(MaterialWriteRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadMaterialLookups(ct); return View("MaterialForm", request); }
        try { await api.PostAsync<MaterialWriteRequest, MaterialDto>("api/materials", request, ct); TempData["Notice"] = $"物料 {request.MaterialNo} 已建立。"; return RedirectToAction(nameof(Materials), new { selected = request.MaterialNo }); }
        catch (HttpRequestException ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadMaterialLookups(ct); return View("MaterialForm", request); }
    }

    [HttpGet]
    public async Task<IActionResult> EditMaterial(string id, CancellationToken ct)
    {
        var item = await api.GetAsync<MaterialDto>($"api/materials/{E(id)}", ct);
        if (item is null) return NotFound();
        if (item.IssueState != 0) return Conflict("只有 UNFROZEN 物料可以編輯。");
        await LoadMaterialLookups(ct);
        ViewData["CreatedAt"] = item.CreatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
        ViewData["RevisedAt"] = "";
        return View("MaterialForm", ToRequest(item));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMaterial(string id, MaterialWriteRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadMaterialLookups(ct); return View("MaterialForm", request); }
        try { await api.PutAsync<MaterialWriteRequest, MaterialDto>($"api/materials/{E(id)}", request, ct); TempData["Notice"] = $"物料 {id} 已更新。"; return RedirectToAction(nameof(Materials), new { selected = id }); }
        catch (HttpRequestException ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadMaterialLookups(ct); return View("MaterialForm", request); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> ApproveMaterial(string no, int stamp, CancellationToken ct) => Approve("materials", no, stamp, nameof(Materials), ct);
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteMaterial(string no, int state, int stamp, CancellationToken ct) => Remove("materials", no, null, state, stamp, nameof(Materials), ct);

    [HttpGet]
    public async Task<IActionResult> Processes(string? keyword, int? issueState, string? no, string? version, CancellationToken ct)
    {
        try
        {
            issueState ??= 0;
            var items = await api.GetListAsync<ProcessDto>(Query("api/processes", keyword, issueState), ct);
            var selected = items.FirstOrDefault(x => Eq(x.ProcessNo, no) && Eq(x.ProcessVersion, version)) ?? items.FirstOrDefault();
            var graph = selected is null ? null : await api.GetAsync<ProcessGraphDto>($"api/processes/{E(selected.ProcessNo)}/{E(selected.ProcessVersion)}/graph", ct);
            return View(new ProcessIndexViewModel { Items = items, Selected = selected, Keyword = keyword, IssueState = issueState, Graph = graph });
        }
        catch (HttpRequestException) { return View("ApiUnavailable"); }
    }

    [HttpGet]
    public async Task<IActionResult> NewProcess(CancellationToken ct) { await LoadProcessLookups(ct); return View("ProcessForm", new ProcessWriteRequest { ProcessVersion = "01", ProcessType = "PRODUCTION", ProductionSegmentNo = "FRONT", ProcessClass = 1 }); }

    [HttpGet]
    public async Task<IActionResult> CopyProcess(string no, string version, CancellationToken ct)
    {
        var item = await api.GetAsync<ProcessDto>($"api/processes/{E(no)}/{E(version)}", ct);
        if (item is null) return NotFound();
        await LoadProcessLookups(ct);
        var request = ToRequest(item); request.ProcessNo = ""; request.ProcessVersion = "01"; request.DataStamp = 0;
        ViewData["IsCopy"] = true;
        return View("ProcessForm", request);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NewProcess(ProcessWriteRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadProcessLookups(ct); return View("ProcessForm", request); }
        try { await api.PostAsync<ProcessWriteRequest, ProcessDto>("api/processes", request, ct); TempData["Notice"] = $"流程 {request.ProcessNo}/{request.ProcessVersion} 已建立，請編輯流程節點。"; return RedirectToAction(nameof(Processes), new { no = request.ProcessNo, version = request.ProcessVersion }); }
        catch (HttpRequestException ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadProcessLookups(ct); return View("ProcessForm", request); }
    }

    [HttpGet]
    public async Task<IActionResult> EditProcess(string no, string version, CancellationToken ct)
    {
        var item = await api.GetAsync<ProcessDto>($"api/processes/{E(no)}/{E(version)}", ct);
        if (item is null) return NotFound();
        if (item.IssueState != 0) return Conflict("只有 UNFROZEN 流程可以編輯。");
        await LoadProcessLookups(ct);
        ViewData["CreatedAt"] = item.CreatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
        ViewData["RevisedAt"] = "";
        return View("ProcessForm", ToRequest(item));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProcess(string no, string version, ProcessWriteRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadProcessLookups(ct); return View("ProcessForm", request); }
        try { await api.PutAsync<ProcessWriteRequest, ProcessDto>($"api/processes/{E(no)}/{E(version)}", request, ct); TempData["Notice"] = $"流程 {no}/{version} 已更新。"; return RedirectToAction(nameof(Processes), new { no, version }); }
        catch (HttpRequestException ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadProcessLookups(ct); return View("ProcessForm", request); }
    }

    [HttpGet]
    public async Task<IActionResult> EditGraph(string no, string version, CancellationToken ct)
    {
        var process = await api.GetAsync<ProcessDto>($"api/processes/{E(no)}/{E(version)}", ct);
        if (process is null) return NotFound();
        if (process.IssueState != 0) return Conflict("只有 UNFROZEN 流程可以編輯節點。");
        var graph = await api.GetAsync<ProcessGraphDto>($"api/processes/{E(no)}/{E(version)}/graph", ct);
        if (graph is null) return NotFound();
        var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        return View("ProcessGraph", new ProcessGraphPageModel { Process = process, Graph = graph, NodesJson = JsonSerializer.Serialize(graph.Nodes, options), EdgesJson = JsonSerializer.Serialize(graph.Edges, options) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditGraph(string no, string version, int dataStamp, string nodesJson, string edgesJson, CancellationToken ct)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var nodes = JsonSerializer.Deserialize<List<ProcessNodeDto>>(nodesJson, jsonOptions) ?? [];
            var edges = JsonSerializer.Deserialize<List<ProcessEdgeDto>>(edgesJson, jsonOptions) ?? [];
            await api.PutAsync<ProcessGraphDto, ProcessGraphDto>($"api/processes/{E(no)}/{E(version)}/graph", new ProcessGraphDto(nodes, edges, dataStamp), ct);
            TempData["Notice"] = $"流程 {no}/{version} 的節點已儲存。";
            return RedirectToAction(nameof(Processes), new { no, version });
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var process = await api.GetAsync<ProcessDto>($"api/processes/{E(no)}/{E(version)}", ct);
            var graph = await api.GetAsync<ProcessGraphDto>($"api/processes/{E(no)}/{E(version)}/graph", ct);
            if (process is null || graph is null) return NotFound();
            return View("ProcessGraph", new ProcessGraphPageModel { Process = process, Graph = graph, NodesJson = nodesJson, EdgesJson = edgesJson });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveProcess(string no, string version, int stamp, CancellationToken ct)
    {
        try { await api.PostAsync<object, ProcessDto>($"api/processes/{E(no)}/{E(version)}/approve", new { DataStamp = stamp }, ct); TempData["Notice"] = $"流程 {no}/{version} 已核准。"; }
        catch (HttpRequestException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Processes), new { no, version });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NewProcessVersion(string no, string version, CancellationToken ct)
    {
        try { var x = await api.PostAsync<object, ProcessDto>($"api/processes/{E(no)}/{E(version)}/new-version", new { }, ct); TempData["Notice"] = $"已複製新版本 {x.ProcessVersion}。"; return RedirectToAction(nameof(Processes), new { no = x.ProcessNo, version = x.ProcessVersion }); }
        catch (HttpRequestException ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Processes), new { no, version }); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCurrentProcess(string no, string version, int stamp, CancellationToken ct)
    {
        try { await api.PostAsync<object, ProcessDto>($"api/processes/{E(no)}/{E(version)}/set-current", new { DataStamp = stamp }, ct); TempData["Notice"] = $"流程 {no}/{version} 已設為目前版本。"; }
        catch (HttpRequestException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Processes), new { no, version });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteProcess(string no, string version, int state, int stamp, CancellationToken ct) => Remove("processes", no, version, state, stamp, nameof(Processes), ct, new { no, version });

    [HttpGet]
    public async Task<IActionResult> Products(string? keyword, int? issueState, string? no, string? version, CancellationToken ct)
    {
        try
        {
            issueState ??= 0;
            var items = await api.GetListAsync<ProductDto>(Query("api/products", keyword, issueState), ct);
            var selected = items.FirstOrDefault(x => Eq(x.ProductNo, no) && Eq(x.ProductVersion, version)) ?? items.FirstOrDefault();
            var processes = selected is null ? [] : await api.GetListAsync<ProductProcessDto>($"api/products/{E(selected.ProductNo)}/versions/{E(selected.ProductVersion)}/processes", ct);
            var bom = selected is null ? [] : await api.GetListAsync<ProductBomDto>($"api/products/{E(selected.ProductNo)}/versions/{E(selected.ProductVersion)}/bom", ct);
            var processOptions = await api.GetListAsync<ProcessDto>("api/processes?issueState=2", ct);
            var materials = await api.GetListAsync<MaterialDto>("api/materials?issueState=2", ct);
            var operations = await api.GetListAsync<OperationDto>("api/operations?issueState=2", ct);
            return View(new ProductIndexViewModel { Items = items, Selected = selected, Processes = processes, Bom = bom, AvailableProcesses = processOptions, AvailableMaterials = materials, AvailableOperations = operations, Keyword = keyword, IssueState = issueState });
        }
        catch (HttpRequestException) { return View("ApiUnavailable"); }
    }

    [HttpGet]
    public async Task<IActionResult> NewProduct(CancellationToken ct) { await LoadProductLookups(ct); return View("ProductForm", new ProductWriteRequest { ProductVersion = "01", ProductType = "PRODUCT", UnitType = "PCS", UnitNo = "PCS", LotStdQty = 1 }); }

    [HttpGet]
    public async Task<IActionResult> CopyProduct(string no, string version, CancellationToken ct)
    {
        var item = await api.GetAsync<ProductDto>($"api/products/{E(no)}/versions/{E(version)}", ct);
        if (item is null) return NotFound();
        await LoadProductLookups(ct);
        var request = ToRequest(item); request.ProductNo = ""; request.ProductVersion = "01"; request.DataStamp = 0;
        ViewData["IsCopy"] = true;
        return View("ProductForm", request);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NewProduct(ProductWriteRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadProductLookups(ct); return View("ProductForm", request); }
        try { await api.PostAsync<ProductWriteRequest, ProductDto>("api/products", request, ct); TempData["Notice"] = $"產品 {request.ProductNo}/{request.ProductVersion} 已建立。"; return RedirectToAction(nameof(Products), new { no = request.ProductNo, version = request.ProductVersion }); }
        catch (HttpRequestException ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadProductLookups(ct); return View("ProductForm", request); }
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(string no, string version, CancellationToken ct)
    {
        var item = await api.GetAsync<ProductDto>($"api/products/{E(no)}/versions/{E(version)}", ct);
        if (item is null) return NotFound();
        if (item.IssueState != 0) return Conflict("只有 UNFROZEN 產品可以編輯。");
        await LoadProductLookups(ct);
        ViewData["CreatedAt"] = item.CreatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
        ViewData["RevisedAt"] = "";
        return View("ProductForm", ToRequest(item));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(string no, string version, ProductWriteRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadProductLookups(ct); return View("ProductForm", request); }
        try { await api.PutAsync<ProductWriteRequest, ProductDto>($"api/products/{E(no)}/versions/{E(version)}", request, ct); TempData["Notice"] = $"產品 {no}/{version} 已更新。"; return RedirectToAction(nameof(Products), new { no, version }); }
        catch (HttpRequestException ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadProductLookups(ct); return View("ProductForm", request); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveProduct(string no, string version, int stamp, CancellationToken ct)
    {
        try { await api.PostAsync<object, ProductDto>($"api/products/{E(no)}/versions/{E(version)}/approve", new { DataStamp = stamp }, ct); TempData["Notice"] = $"產品 {no}/{version} 已核准。"; }
        catch (HttpRequestException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Products), new { no, version });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NewProductVersion(string no, string version, CancellationToken ct)
    {
        try { var x = await api.PostAsync<object, ProductDto>($"api/products/{E(no)}/versions/{E(version)}/new-version", new { }, ct); TempData["Notice"] = $"已複製新產品版本 {x.ProductVersion}。"; return RedirectToAction(nameof(Products), new { no = x.ProductNo, version = x.ProductVersion }); }
        catch (HttpRequestException ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Products), new { no, version }); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCurrentProduct(string no, string version, int stamp, CancellationToken ct)
    {
        try { await api.PostAsync<object, ProductDto>($"api/products/{E(no)}/versions/{E(version)}/set-current", new { DataStamp = stamp }, ct); TempData["Notice"] = $"產品 {no}/{version} 已設為目前版本。"; }
        catch (HttpRequestException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Products), new { no, version });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteProduct(string no, string version, int state, int stamp, CancellationToken ct) => Remove("products", no, version, state, stamp, nameof(Products), ct, new { no, version });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProductProcess(string no, string version, ProductProcessWriteRequest request, CancellationToken ct)
    {
        try { await api.PostAsync<ProductProcessWriteRequest, ProductProcessDto>($"api/products/{E(no)}/versions/{E(version)}/processes", request, ct); TempData["Notice"] = "產品流程已加入。"; }
        catch (HttpRequestException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Products), new { no, version });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProductProcess(string no, string version, int moTypeNo, string segment, string processNo, string selector, CancellationToken ct)
    {
        var path = $"api/products/{E(no)}/versions/{E(version)}/processes/{E(processNo)}/{E(selector)}?moTypeNo={moTypeNo}&productionSegmentNo={E(segment)}";
        try { await api.DeleteAsync(path, ct); TempData["Notice"] = "產品流程已移除。"; }
        catch (HttpRequestException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Products), new { no, version });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBom(string no, string version, ProductBomWriteRequest request, CancellationToken ct)
    {
        try { await api.PostAsync<ProductBomWriteRequest, ProductBomDto>($"api/products/{E(no)}/versions/{E(version)}/bom", request, ct); TempData["Notice"] = "BOM 項目已加入。"; }
        catch (HttpRequestException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Products), new { no, version });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBom(string no, string version, string bomId, CancellationToken ct)
    {
        try { await api.DeleteAsync($"api/products/{E(no)}/versions/{E(version)}/bom/{E(bomId)}", ct); TempData["Notice"] = "BOM 項目已移除。"; }
        catch (HttpRequestException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Products), new { no, version });
    }

    private async Task<IActionResult> Approve(string resource, string no, int stamp, string redirect, CancellationToken ct)
    {
        try { await api.PostAsync<object, JsonElement>($"api/{resource}/{E(no)}/approve", new { DataStamp = stamp }, ct); TempData["Notice"] = $"{no} 已核准。"; }
        catch (HttpRequestException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(redirect, new { selected = no });
    }

    private async Task<IActionResult> Remove(string resource, string no, string? version, int state, int stamp, string redirect, CancellationToken ct, object? routeValues = null)
    {
        var path = version is null
            ? $"api/{resource}/{E(no)}?issueState={state}&dataStamp={stamp}"
            : resource == "products"
                ? $"api/products/{E(no)}/versions/{E(version)}?issueState={state}&dataStamp={stamp}"
                : $"api/{resource}/{E(no)}/{E(version)}?issueState={state}&dataStamp={stamp}";
        try { await api.DeleteAsync(path, ct); TempData["Notice"] = state == 2 ? $"{no} 已停用。" : $"{no} 已刪除。"; }
        catch (HttpRequestException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(redirect, routeValues ?? new { selected = no });
    }

    private async Task LoadMaterialLookups(CancellationToken ct)
    {
        try
        {
            var items = await api.GetListAsync<MaterialDto>("api/materials", ct);
            ViewData["MaterialTypes"] = items.Select(x => x.MaterialType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
            ViewData["PutInPlaces"] = items.Select(x => x.PutInPlace).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
            ViewData["UnitTypes"] = items.Select(x => x.UnitType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
            ViewData["UnitNos"] = items.Select(x => x.UnitNo).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        }
        catch (HttpRequestException) { }
    }
    private async Task LoadOperationLookups(CancellationToken ct)
    {
        try
        {
            var operations = await api.GetListAsync<OperationDto>("api/operations", ct);
            var processes = await api.GetListAsync<ProcessDto>("api/processes", ct);
            ViewData["OperationTypes"] = operations.Select(x => x.OperationType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
            ViewData["ProductionSegments"] = operations.Select(x => x.ProductionSegmentNo).Concat(processes.Select(x => x.ProductionSegmentNo)).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        }
        catch (HttpRequestException) { }
    }

    private async Task LoadProcessLookups(CancellationToken ct)
    {
        try
        {
            var items = await api.GetListAsync<ProcessDto>("api/processes", ct);
            ViewData["ProcessTypes"] = items.Select(x => x.ProcessType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
            ViewData["ProductionSegments"] = items.Select(x => x.ProductionSegmentNo).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        }
        catch (HttpRequestException) { }
    }

    private async Task LoadProductLookups(CancellationToken ct)
    {
        try
        {
            var products = await api.GetListAsync<ProductDto>("api/products", ct);
            var materials = await api.GetListAsync<MaterialDto>("api/materials", ct);
            ViewData["ProductTypes"] = products.Select(x => x.ProductType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
            ViewData["UnitTypes"] = products.Select(x => x.UnitType).Concat(materials.Select(x => x.UnitType)).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
            ViewData["UnitNos"] = products.Select(x => x.UnitNo).Concat(materials.Select(x => x.UnitNo)).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
            ViewData["SerialTypesLot"] = products.Select(x => x.SerialTypeNoLot).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
            ViewData["SerialTypesComp"] = products.Select(x => x.SerialTypeNoComp).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        }
        catch (HttpRequestException) { }
    }
    private static string Query(string path, string? keyword, int? state) => $"{path}?keyword={Uri.EscapeDataString(keyword ?? "")}&issueState={state?.ToString() ?? ""}";
    private static string E(string value) => Uri.EscapeDataString(value);
    private static bool Eq(string a, string? b) => b is not null && a.Equals(b, StringComparison.OrdinalIgnoreCase);

    private static OperationWriteRequest ToRequest(OperationDto x) => new() { OperationNo = x.OperationNo, OperationName = x.OperationName, OperationType = x.OperationType, OperationClass = x.OperationClass, ShortName = x.ShortName, OperationOrder = x.OperationOrder, ProductionSegmentNo = x.ProductionSegmentNo, Description = x.Description, MaterialOption = x.MaterialOption, MultiOperatorMode = x.MultiOperatorMode, PrintOutOnRunCard = x.PrintOutOnRunCard, SpcInspAbnormalHoldLot = x.SpcInspAbnormalHoldLot, ErrorQtyToSpc = x.ErrorQtyToSpc, StandardUnitRunTime = x.StandardUnitRunTime, CountOperationUnitQty = x.CountOperationUnitQty, StandardQueueTime = x.StandardQueueTime, DataStamp = x.DataStamp };
    private static MaterialWriteRequest ToRequest(MaterialDto x) => new() { MaterialNo = x.MaterialNo, MaterialType = x.MaterialType, MaterialName = x.MaterialName, MaterialSpec = x.MaterialSpec, Description = x.Description, PutInPlace = x.PutInPlace, CountWay = x.CountWay, CheckLotNo = x.CheckLotNo, UnitType = x.UnitType, UnitNo = x.UnitNo, SafeQty = x.SafeQty, KeyMaterials = x.KeyMaterials, OpenLimitTimeFlag = x.OpenLimitTimeFlag, OpenLimitTime = x.OpenLimitTime, ResetOpenDate = x.ResetOpenDate, ExpireDateFlag = x.ExpireDateFlag, ErpNo = x.ErpNo, DataStamp = x.DataStamp };
    private static ProcessWriteRequest ToRequest(ProcessDto x) => new() { ProcessNo = x.ProcessNo, ProcessVersion = x.ProcessVersion, ProcessType = x.ProcessType, ProductionSegmentNo = x.ProductionSegmentNo, ProcessClass = x.ProcessClass, Description = x.Description, TuningProcess = x.TuningProcess, PartialType = x.PartialType, DataStamp = x.DataStamp };
    private static ProductWriteRequest ToRequest(ProductDto x) => new() { ProductNo = x.ProductNo, ProductVersion = x.ProductVersion, ProductName = x.ProductName, ProductType = x.ProductType, UnitType = x.UnitType, UnitNo = x.UnitNo, Description = x.Description, SpecNo = x.SpecNo, ProductCode = x.ProductCode, SerialTypeNoLot = x.SerialTypeNoLot, SerialTypeNoComp = x.SerialTypeNoComp, LotStdQty = x.LotStdQty, ErpNo = x.ErpNo, DataStamp = x.DataStamp };
}
