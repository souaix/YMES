using MES.Contracts;

namespace MES.Api.Data;

public sealed class FakeMasterDataRepository : IMasterDataRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, OperationDto> _operations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MaterialDto> _materials = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string No, string Version), ProcessDto> _processes = [];
    private readonly Dictionary<(string No, string Version), ProcessGraphDto> _graphs = [];
    private readonly Dictionary<(string No, string Version), ProductDto> _products = [];
    private readonly List<ProductProcessDto> _productProcesses = [];
    private readonly List<ProductBomDto> _bom = [];

    public FakeMasterDataRepository()
    {
        var now = DateTime.UtcNow;
        SeedOperation("OP-CLEAN", "清洗", "CLEAN", "FRONT", 2, now);
        SeedOperation("OP-ETCH", "蝕刻", "ETCH", "FRONT", 2, now);
        SeedOperation("OP-QC", "外觀檢查", "INSPECT", "FRONT", 2, now);
        SeedOperation("OP-PACK", "包裝", "PACK", "ASSY", 0, now);

        SeedMaterial("MAT-WAFER-300", "WAFER", "300mm Silicon Wafer", "Prime grade / 300 mm", "PCS", "PCS", 2, now);
        SeedMaterial("MAT-ETCH-GAS", "CHEMICAL", "Etch Gas A", "Process gas", "CYL", "CYL", 2, now);
        SeedMaterial("MAT-PACK-TRAY", "PACKING", "ESD Carrier Tray", "12-inch tray", "PCS", "PCS", 0, now);

        SeedProcess("PR-ETCH", "01", "ETCH", "FRONT", 1, "Etch production route", true, 2, now,
            [new("start", "START", "START", 0, "", "開始"), new("etch", "OP10", "OP", 10, "OP-ETCH", "蝕刻站"), new("qc", "OP20", "OP", 20, "OP-QC", "檢查站"), new("end", "END", "END", 30, "", "結束")],
            [new("start", "etch", "pass"), new("etch", "qc", "pass"), new("qc", "end", "pass")]);
        SeedProcess("PR-CLEAN", "01", "CLEAN", "FRONT", 1, "Cleaning route", true, 2, now,
            [new("start", "START", "START", 0, "", "開始"), new("clean", "OP10", "OP", 10, "OP-CLEAN", "清洗站"), new("end", "END", "END", 20, "", "結束")],
            [new("start", "clean", "pass"), new("clean", "end", "pass")]);
        SeedProcess("PR-ETCH", "02", "ETCH", "FRONT", 1, "Etch route revision", false, 0, now,
            [new("start", "START", "START", 0, "", "開始"), new("etch", "OP10", "OP", 10, "OP-ETCH", "蝕刻站"), new("end", "END", "END", 20, "", "結束")],
            [new("start", "etch", "pass"), new("etch", "end", "pass")]);

        SeedProduct("PRD-1000", "01", "Logic Wafer A", "PRODUCT", "PCS", "PCS", 100, true, 2, now);
        SeedProduct("PRD-2000", "01", "Logic Wafer B", "PRODUCT", "PCS", "PCS", 100, false, 0, now);
        _productProcesses.Add(new("PRD-1000", "01", 10, "FRONT", 10, "PR-ETCH", "*", "01", true, true, true));
        _productProcesses.Add(new("PRD-1000", "01", 10, "BACK", 20, "PR-CLEAN", "01", "01", true, false, false));
        _bom.Add(new("BOM-1001", "PRD-1000", "01", "MATERIAL", "MAT-WAFER-300", "OP-ETCH", 1, "At OP", false, 0, "Main wafer"));
        _bom.Add(new("BOM-1002", "PRD-1000", "01", "MATERIAL", "MAT-ETCH-GAS", "OP-ETCH", 0.25m, "At OP", false, 0, "Etch gas"));
    }

    public IReadOnlyList<OperationDto> ListOperations(string? keyword, int? issueState) => Read(() => _operations.Values
        .Where(x => issueState is null || x.IssueState == issueState)
        .Where(x => ContainsAny(keyword, x.OperationNo))
        .OrderBy(x => x.OperationNo, StringComparer.OrdinalIgnoreCase).ToArray());

    public OperationDto GetOperation(string no) => Read(() => _operations.TryGetValue(Key(no), out var x) ? x : throw NotFound("作業", no));

    public OperationDto CreateOperation(OperationWriteRequest request) => Write(() =>
    {
        var no = Key(request.OperationNo);
        ValidateOperationNo(no);
        if (_operations.ContainsKey(no)) throw Conflict($"作業編號 {no} 已存在。");
        var item = MapOperation(request, 0, 1, DateTime.UtcNow);
        _operations.Add(no, item);
        return item;
    });

    public OperationDto UpdateOperation(string no, OperationWriteRequest request) => Write(() =>
    {
        var current = GetOperationUnsafe(no);
        CheckEditable(current.IssueState, current.DataStamp, request.DataStamp);
        if (!Key(no).Equals(Key(request.OperationNo), StringComparison.OrdinalIgnoreCase)) throw BadRequest("更新時不可變更 OperationNo。");
        ValidateOperationNo(Key(no));
        var item = MapOperation(request, 0, current.DataStamp + 1, current.CreatedAt);
        _operations[Key(no)] = item;
        return item;
    });

    public OperationDto ApproveOperation(string no, int dataStamp) => Write(() =>
    {
        var current = GetOperationUnsafe(no);
        CheckStamp(current.DataStamp, dataStamp);
        if (current.IssueState != 0) throw Conflict("只有 UNFROZEN 作業可以核准。");
        var item = current with { IssueState = 2, DataStamp = current.DataStamp + 1 };
        _operations[Key(no)] = item;
        return item;
    });

    public void DeleteOperation(string no, int issueState, int dataStamp) => Write(() =>
    {
        var item = GetOperationUnsafe(no);
        CheckRemoval(item.IssueState, issueState, item.DataStamp, dataStamp);
        if (item.IssueState == 2)
        {
            _operations[Key(no)] = item with { IssueState = -1, DataStamp = item.DataStamp + 1 };
            return true;
        }
        if (_graphs.Values.Any(g => g.Nodes.Any(n => n.OperationNo.Equals(no, StringComparison.OrdinalIgnoreCase))) ||
            _bom.Any(x => x.InputOperationNo.Equals(no, StringComparison.OrdinalIgnoreCase)))
            throw Conflict("作業仍被流程節點或產品 BOM 使用，不能物理刪除。");
        _operations.Remove(Key(no));
        return true;
    });

    public IReadOnlyList<MaterialDto> ListMaterials(string? keyword, int? issueState, string? materialType) => Read(() => _materials.Values
        .Where(x => issueState is null || x.IssueState == issueState)
        .Where(x => string.IsNullOrWhiteSpace(materialType) || x.MaterialType.Equals(materialType, StringComparison.OrdinalIgnoreCase))
        .Where(x => ContainsAny(keyword, x.MaterialNo))
        .OrderBy(x => x.MaterialNo, StringComparer.OrdinalIgnoreCase).ToArray());

    public MaterialDto GetMaterial(string no) => Read(() => _materials.TryGetValue(Key(no), out var x) ? x : throw NotFound("物料", no));

    public MaterialDto CreateMaterial(MaterialWriteRequest request) => Write(() =>
    {
        var no = Key(request.MaterialNo);
        if (_materials.ContainsKey(no)) throw Conflict($"物料編號 {no} 已存在。");
        if (_operations.ContainsKey(no)) throw Conflict("物料編號不可與作業編號重複。");
        if (_products.Keys.Any(x => x.No.Equals(no, StringComparison.OrdinalIgnoreCase))) throw Conflict("物料編號不可與產品編號重複。");
        var item = MapMaterial(request, 0, 1, DateTime.UtcNow);
        _materials.Add(no, item);
        return item;
    });

    public MaterialDto UpdateMaterial(string no, MaterialWriteRequest request) => Write(() =>
    {
        var current = GetMaterialUnsafe(no);
        CheckEditable(current.IssueState, current.DataStamp, request.DataStamp);
        if (!Key(no).Equals(Key(request.MaterialNo), StringComparison.OrdinalIgnoreCase)) throw BadRequest("更新時不可變更 MaterialNo。");
        var item = MapMaterial(request, 0, current.DataStamp + 1, current.CreatedAt);
        _materials[Key(no)] = item;
        return item;
    });

    public MaterialDto ApproveMaterial(string no, int dataStamp) => Write(() =>
    {
        var current = GetMaterialUnsafe(no);
        CheckStamp(current.DataStamp, dataStamp);
        if (current.IssueState != 0) throw Conflict("只有 UNFROZEN 物料可以核准。");
        var item = current with { IssueState = 2, DataStamp = current.DataStamp + 1 };
        _materials[Key(no)] = item;
        return item;
    });

    public void DeleteMaterial(string no, int issueState, int dataStamp) => Write(() =>
    {
        var item = GetMaterialUnsafe(no);
        CheckRemoval(item.IssueState, issueState, item.DataStamp, dataStamp);
        if (item.IssueState == 2)
        {
            _materials[Key(no)] = item with { IssueState = -1, DataStamp = item.DataStamp + 1 };
            return true;
        }
        if (_bom.Any(x => x.ComponentType == "MATERIAL" && x.ComponentNo.Equals(no, StringComparison.OrdinalIgnoreCase)))
            throw Conflict("物料仍被產品 BOM 使用，不能物理刪除。");
        _materials.Remove(Key(no));
        return true;
    });

    public IReadOnlyList<ProcessDto> ListProcesses(string? keyword, int? issueState) => Read(() => _processes.Values
        .Where(x => issueState is null || x.IssueState == issueState)
        .Where(x => ContainsAny(keyword, x.ProcessNo))
        .OrderBy(x => x.ProcessNo, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.ProcessVersion, StringComparer.OrdinalIgnoreCase).ToArray());

    public ProcessDto GetProcess(string no, string version) => Read(() => GetProcessUnsafe(no, version));

    public ProcessDto CreateProcess(ProcessWriteRequest request) => Write(() =>
    {
        var no = Key(request.ProcessNo);
        var version = Key(request.ProcessVersion);
        if (no.Contains(',') || version.Contains(',')) throw BadRequest("ProcessNo 與 ProcessVersion 不可包含逗號。");
        if (_processes.ContainsKey((no, version))) throw Conflict($"流程 {no}/{version} 已存在。");
        var item = MapProcess(request, 0, false, 1, DateTime.UtcNow);
        _processes[(no, version)] = item;
        _graphs[(no, version)] = new ProcessGraphDto([new("start", "START", "START", 0, "", "開始"), new("end", "END", "END", 10, "", "結束")], [new("start", "end", "pass")], 1);
        return item;
    });

    public ProcessDto UpdateProcess(string no, string version, ProcessWriteRequest request) => Write(() =>
    {
        var key = (Key(no), Key(version));
        var current = GetProcessUnsafe(no, version);
        CheckEditable(current.IssueState, current.DataStamp, request.DataStamp);
        if (!Key(request.ProcessNo).Equals(key.Item1, StringComparison.OrdinalIgnoreCase) || !Key(request.ProcessVersion).Equals(key.Item2, StringComparison.OrdinalIgnoreCase))
            throw BadRequest("更新時不可變更 ProcessNo/ProcessVersion。");
        var item = MapProcess(request, 0, current.CurrentVersion, current.DataStamp + 1, current.CreatedAt);
        _processes[key] = item;
        return item;
    });

    public ProcessDto ApproveProcess(string no, string version, int dataStamp) => Write(() =>
    {
        var key = (Key(no), Key(version));
        var current = GetProcessUnsafe(no, version);
        CheckStamp(current.DataStamp, dataStamp);
        if (current.IssueState != 0) throw Conflict("只有 UNFROZEN 流程可以核准。");
        ValidateGraph(_graphs[key]);
        var item = current with { IssueState = 2, DataStamp = current.DataStamp + 1 };
        _processes[key] = item;
        return item;
    });

    public ProcessGraphDto GetGraph(string no, string version) => Read(() =>
    {
        var key = (Key(no), Key(version));
        var process = GetProcessUnsafe(no, version);
        return _graphs.TryGetValue(key, out var graph) ? graph with { DataStamp = process.DataStamp } : throw NotFound("流程圖", $"{no}/{version}");
    });

    public ProcessGraphDto UpdateGraph(string no, string version, ProcessGraphDto request) => Write(() =>
    {
        var key = (Key(no), Key(version));
        var process = GetProcessUnsafe(no, version);
        if (process.IssueState != 0) throw Conflict("只有 UNFROZEN 流程可以編輯流程圖。");
        CheckStamp(process.DataStamp, request.DataStamp);
        ValidateGraph(request);
        var saved = request with { Nodes = request.Nodes.OrderBy(x => x.Sequence).ToArray(), DataStamp = request.DataStamp + 1 };
        _graphs[key] = saved;
        _processes[key] = process with { DataStamp = process.DataStamp + 1 };
        return saved;
    });

    public ProcessDto CreateProcessVersion(string no, string version) => Write(() =>
    {
        var oldKey = (Key(no), Key(version));
        var old = GetProcessUnsafe(no, version);
        if (!int.TryParse(old.ProcessVersion, out var currentVersion)) throw BadRequest("版本格式不是數字，無法自動產生下一版。");
        var nextVersion = (currentVersion + 1).ToString(new string('0', Math.Max(2, old.ProcessVersion.Length)));
        var nextKey = (oldKey.Item1, nextVersion);
        if (_processes.ContainsKey(nextKey)) throw Conflict($"流程 {no}/{nextVersion} 已存在。");
        var newProcess = old with { ProcessVersion = nextVersion, IssueState = 0, CurrentVersion = false, DataStamp = 1, CreatedAt = DateTime.UtcNow };
        _processes[nextKey] = newProcess;
        var oldGraph = _graphs[oldKey];
        _graphs[nextKey] = oldGraph with { DataStamp = 1, Nodes = oldGraph.Nodes.ToArray(), Edges = oldGraph.Edges.ToArray() };
        return newProcess;
    });

    public ProcessDto SetCurrentProcess(string no, string version, int dataStamp) => Write(() =>
    {
        var key = (Key(no), Key(version));
        var current = GetProcessUnsafe(no, version);
        CheckStamp(current.DataStamp, dataStamp);
        if (current.IssueState != 2) throw Conflict("只有 ACTIVE 流程版本可以設為目前版本。");
        foreach (var old in _processes.Where(x => x.Key.No.Equals(key.Item1, StringComparison.OrdinalIgnoreCase) && x.Value.CurrentVersion).ToArray())
            _processes[old.Key] = old.Value with { CurrentVersion = false, DataStamp = old.Value.DataStamp + 1 };
        var updated = current with { CurrentVersion = true, DataStamp = current.DataStamp + 1 };
        _processes[key] = updated;
        return updated;
    });

    public void DeleteProcess(string no, string version, int issueState, int dataStamp) => Write(() =>
    {
        var key = (Key(no), Key(version));
        var item = GetProcessUnsafe(no, version);
        CheckRemoval(item.IssueState, issueState, item.DataStamp, dataStamp);
        if (item.IssueState == 2)
        {
            _processes[key] = item with { IssueState = -1, CurrentVersion = false, DataStamp = item.DataStamp + 1 };
            return true;
        }
        if (_productProcesses.Any(x => x.ProcessNo.Equals(no, StringComparison.OrdinalIgnoreCase) && (x.ProcessVersionSelector == "*" || x.ProcessVersionSelector.Equals(version, StringComparison.OrdinalIgnoreCase))))
            throw Conflict("流程仍被產品流程使用，不能物理刪除。");
        _processes.Remove(key);
        _graphs.Remove(key);
        return true;
    });

    public IReadOnlyList<ProductDto> ListProducts(string? keyword, int? issueState) => Read(() => _products.Values
        .Where(x => issueState is null || x.IssueState == issueState)
        .Where(x => ContainsAny(keyword, x.ProductNo))
        .OrderBy(x => x.ProductNo, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.ProductVersion, StringComparer.OrdinalIgnoreCase).ToArray());

    public ProductDto GetProduct(string no, string version) => Read(() => GetProductUnsafe(no, version));

    public ProductDto CreateProduct(ProductWriteRequest request) => Write(() =>
    {
        var no = Key(request.ProductNo);
        var version = Key(request.ProductVersion);
        if (_products.ContainsKey((no, version))) throw Conflict($"產品 {no}/{version} 已存在。");
        if (_materials.ContainsKey(no)) throw Conflict("產品編號不可與物料編號重複。");
        var item = MapProduct(request, 0, false, 1, DateTime.UtcNow);
        _products[(no, version)] = item;
        return item;
    });

    public ProductDto UpdateProduct(string no, string version, ProductWriteRequest request) => Write(() =>
    {
        var key = (Key(no), Key(version));
        var current = GetProductUnsafe(no, version);
        CheckEditable(current.IssueState, current.DataStamp, request.DataStamp);
        if (!Key(request.ProductNo).Equals(key.Item1, StringComparison.OrdinalIgnoreCase) || !Key(request.ProductVersion).Equals(key.Item2, StringComparison.OrdinalIgnoreCase))
            throw BadRequest("更新時不可變更 ProductNo/ProductVersion。");
        var item = MapProduct(request, 0, current.CurrentVersion, current.DataStamp + 1, current.CreatedAt);
        _products[key] = item;
        return item;
    });

    public ProductDto ApproveProduct(string no, string version, int dataStamp) => Write(() =>
    {
        var key = (Key(no), Key(version));
        var current = GetProductUnsafe(no, version);
        CheckStamp(current.DataStamp, dataStamp);
        if (current.IssueState != 0) throw Conflict("只有 UNFROZEN 產品可以核准。");
        if (!_productProcesses.Any(x => x.ProductNo.Equals(no, StringComparison.OrdinalIgnoreCase) && x.ProductVersion.Equals(version, StringComparison.OrdinalIgnoreCase) && x.DefaultProcess))
            throw Conflict("產品至少需要一條預設流程才能核准。");
        var item = current with { IssueState = 2, DataStamp = current.DataStamp + 1 };
        _products[key] = item;
        return item;
    });

    public ProductDto SetCurrentProduct(string no, string version, int dataStamp) => Write(() =>
    {
        var key = (Key(no), Key(version));
        var current = GetProductUnsafe(no, version);
        CheckStamp(current.DataStamp, dataStamp);
        if (current.IssueState != 2) throw Conflict("只有 ACTIVE 產品版本可以設為目前版本。");
        foreach (var old in _products.Where(x => x.Key.No.Equals(key.Item1, StringComparison.OrdinalIgnoreCase) && x.Value.CurrentVersion).ToArray())
            _products[old.Key] = old.Value with { CurrentVersion = false, DataStamp = old.Value.DataStamp + 1 };
        var updated = current with { CurrentVersion = true, DataStamp = current.DataStamp + 1 };
        _products[key] = updated;
        return updated;
    });

    public ProductDto CreateProductVersion(string no, string version) => Write(() =>
    {
        var current = GetProductUnsafe(no, version);
        if (!int.TryParse(current.ProductVersion, out var numeric)) throw BadRequest("產品版本格式不是數字。");
        var nextVersion = (numeric + 1).ToString(new string('0', Math.Max(2, current.ProductVersion.Length)));
        var nextKey = (Key(no), nextVersion);
        if (_products.ContainsKey(nextKey)) throw Conflict($"產品 {no}/{nextVersion} 已存在。");
        var next = current with { ProductVersion = nextVersion, IssueState = 0, CurrentVersion = false, DataStamp = 1, CreatedAt = DateTime.UtcNow };
        _products[nextKey] = next;
        foreach (var link in _productProcesses.Where(x => x.ProductNo.Equals(no, StringComparison.OrdinalIgnoreCase) && x.ProductVersion.Equals(version, StringComparison.OrdinalIgnoreCase)).ToArray())
            _productProcesses.Add(link with { ProductVersion = nextVersion });
        foreach (var item in _bom.Where(x => x.ProductNo.Equals(no, StringComparison.OrdinalIgnoreCase) && x.ProductVersion.Equals(version, StringComparison.OrdinalIgnoreCase)).ToArray())
            _bom.Add(item with { Id = Guid.NewGuid().ToString("N"), ProductVersion = nextVersion });
        return next;
    });

    public void DeleteProduct(string no, string version, int issueState, int dataStamp) => Write(() =>
    {
        var key = (Key(no), Key(version));
        var item = GetProductUnsafe(no, version);
        CheckRemoval(item.IssueState, issueState, item.DataStamp, dataStamp);
        if (item.IssueState == 2)
        {
            _products[key] = item with { IssueState = -1, CurrentVersion = false, DataStamp = item.DataStamp + 1 };
            return true;
        }
        if (_bom.Any(x => x.ComponentType == "PRODUCT" && x.ComponentNo.Equals(no, StringComparison.OrdinalIgnoreCase)))
            throw Conflict("產品仍被其他產品 BOM 使用，不能物理刪除。");
        _productProcesses.RemoveAll(x => x.ProductNo.Equals(no, StringComparison.OrdinalIgnoreCase) && x.ProductVersion.Equals(version, StringComparison.OrdinalIgnoreCase));
        _bom.RemoveAll(x => x.ProductNo.Equals(no, StringComparison.OrdinalIgnoreCase) && x.ProductVersion.Equals(version, StringComparison.OrdinalIgnoreCase));
        _products.Remove(key);
        return true;
    });

    public IReadOnlyList<ProductProcessDto> GetProductProcesses(string no, string version) => Read(() =>
    {
        _ = GetProductUnsafe(no, version);
        return _productProcesses.Where(x => x.ProductNo.Equals(no, StringComparison.OrdinalIgnoreCase) && x.ProductVersion.Equals(version, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.SegmentOrder).ThenByDescending(x => x.DefaultProcess).ToArray();
    });

    public ProductProcessDto AddProductProcess(string no, string version, ProductProcessWriteRequest request) => Write(() =>
    {
        var product = GetProductUnsafe(no, version);
        if (product.IssueState != 0) throw Conflict("只有 UNFROZEN 產品可以修改流程指派。");
        var selector = Key(request.ProcessVersionSelector);
        var process = ResolveProcess(request.ProcessNo, selector);
        if (!process.ProductionSegmentNo.Equals(request.ProductionSegmentNo, StringComparison.OrdinalIgnoreCase))
            throw BadRequest("產品流程的製程段必須與流程主檔一致。");
        if (_productProcesses.Any(x => SameAssignment(x, no, version, request.MoTypeNo, request.ProductionSegmentNo, request.ProcessNo, selector)))
            throw Conflict("這條產品流程已存在。");
        ValidateDefault(no, version, request.MoTypeNo, request.ProductionSegmentNo, request.DefaultProcess, null);
        var item = MakeProductProcess(product, request, process, selector);
        _productProcesses.Add(item);
        return item;
    });

    public ProductProcessDto UpdateProductProcess(string no, string version, string processNo, string selector, ProductProcessWriteRequest request) => Write(() =>
    {
        var product = GetProductUnsafe(no, version);
        if (product.IssueState != 0) throw Conflict("只有 UNFROZEN 產品可以修改流程指派。");
        var index = _productProcesses.FindIndex(x => SameAssignment(x, no, version, request.MoTypeNo, request.ProductionSegmentNo, processNo, selector));
        if (index < 0) throw NotFound("產品流程", $"{no}/{version}/{processNo}");
        var resolved = ResolveProcess(request.ProcessNo, request.ProcessVersionSelector);
        ValidateDefault(no, version, request.MoTypeNo, request.ProductionSegmentNo, request.DefaultProcess, index);
        var item = MakeProductProcess(product, request, resolved, Key(request.ProcessVersionSelector));
        _productProcesses[index] = item;
        return item;
    });

    public void DeleteProductProcess(string no, string version, int moTypeNo, string segmentNo, string processNo, string selector) => Write(() =>
    {
        var product = GetProductUnsafe(no, version);
        if (product.IssueState != 0) throw Conflict("只有 UNFROZEN 產品可以刪除流程指派。");
        var removed = _productProcesses.RemoveAll(x => SameAssignment(x, no, version, moTypeNo, segmentNo, processNo, selector));
        if (removed == 0) throw NotFound("產品流程", $"{no}/{version}/{processNo}");
        return true;
    });

    public IReadOnlyList<ProductBomDto> GetBom(string no, string version) => Read(() =>
    {
        _ = GetProductUnsafe(no, version);
        return _bom.Where(x => x.ProductNo.Equals(no, StringComparison.OrdinalIgnoreCase) && x.ProductVersion.Equals(version, StringComparison.OrdinalIgnoreCase)).ToArray();
    });

    public ProductBomDto AddBom(string no, string version, ProductBomWriteRequest request) => Write(() =>
    {
        var product = GetProductUnsafe(no, version);
        if (product.IssueState != 0) throw Conflict("只有 UNFROZEN 產品可以修改 BOM。");
        var type = Key(request.ComponentType).ToUpperInvariant();
        var component = Key(request.ComponentNo);
        if (type == "MATERIAL" && !_materials.ContainsKey(component)) throw BadRequest($"找不到物料 {component}。");
        if (type == "PRODUCT" && !_products.Keys.Any(x => x.No.Equals(component, StringComparison.OrdinalIgnoreCase))) throw BadRequest($"找不到子產品 {component}。");
        if (type is not ("MATERIAL" or "PRODUCT")) throw BadRequest("ComponentType 僅能為 MATERIAL 或 PRODUCT。");
        if (type == "PRODUCT" && component.Equals(no, StringComparison.OrdinalIgnoreCase)) throw BadRequest("產品 BOM 不可引用自己。");
        if (!_operations.ContainsKey(Key(request.InputOperationNo))) throw BadRequest($"找不到投入作業 {request.InputOperationNo}。");
        if (_bom.Any(x => x.ProductNo.Equals(no, StringComparison.OrdinalIgnoreCase) && x.ProductVersion.Equals(version, StringComparison.OrdinalIgnoreCase) && x.ComponentType == type && x.ComponentNo.Equals(component, StringComparison.OrdinalIgnoreCase) && x.InputOperationNo.Equals(request.InputOperationNo, StringComparison.OrdinalIgnoreCase)))
            throw Conflict("同一產品、作業與 component 的 BOM 已存在。");
        var item = new ProductBomDto(Guid.NewGuid().ToString("N"), no, version, type, component, Key(request.InputOperationNo), request.StandardQty, request.PutInPlaceType.Trim(), request.Specified, request.DecreaseRate, request.Description.Trim());
        _bom.Add(item);
        return item;
    });

    public void DeleteBom(string no, string version, string id) => Write(() =>
    {
        var product = GetProductUnsafe(no, version);
        if (product.IssueState != 0) throw Conflict("只有 UNFROZEN 產品可以刪除 BOM。");
        var removed = _bom.RemoveAll(x => x.ProductNo.Equals(no, StringComparison.OrdinalIgnoreCase) && x.ProductVersion.Equals(version, StringComparison.OrdinalIgnoreCase) && x.Id == id);
        if (removed == 0) throw NotFound("BOM", id);
        return true;
    });

    private void ValidateGraph(ProcessGraphDto graph)
    {
        if (graph.Nodes.Count < 2) throw BadRequest("流程圖至少需要 START 與 END 節點。");
        if (graph.Nodes.Select(x => x.NodeId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != graph.Nodes.Count) throw BadRequest("NodeId 不可重複。");
        if (graph.Nodes.Select(x => x.Sequence).Distinct().Count() != graph.Nodes.Count) throw BadRequest("節點順序不可重複。");
        if (graph.Nodes.Count(x => x.NodeType.Equals("START", StringComparison.OrdinalIgnoreCase)) != 1 || graph.Nodes.Count(x => x.NodeType.Equals("END", StringComparison.OrdinalIgnoreCase)) != 1)
            throw BadRequest("流程圖必須且只能有一個 START 和一個 END。");
        var ids = graph.Nodes.Select(x => x.NodeId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in graph.Edges)
        {
            if (!ids.Contains(edge.FromNodeId) || !ids.Contains(edge.ToNodeId)) throw BadRequest("連線的起點與終點都必須存在於流程圖。");
            if (edge.FromNodeId.Equals(edge.ToNodeId, StringComparison.OrdinalIgnoreCase)) throw BadRequest("流程圖不可建立自我連線。");
        }
        foreach (var node in graph.Nodes.Where(x => x.NodeType.Equals("OP", StringComparison.OrdinalIgnoreCase)))
            if (!_operations.ContainsKey(Key(node.OperationNo))) throw BadRequest($"節點 {node.NodeNo} 引用不存在的作業 {node.OperationNo}。");
    }

    private ProcessDto ResolveProcess(string no, string selector)
    {
        var processNo = Key(no);
        if (selector == "*")
            return _processes.Values.SingleOrDefault(x => x.ProcessNo.Equals(processNo, StringComparison.OrdinalIgnoreCase) && x.CurrentVersion && x.IssueState == 2)
                ?? throw BadRequest($"流程 {no} 沒有 ACTIVE 的目前版本。");
        var fixedVersion = GetProcessUnsafe(processNo, selector);
        if (fixedVersion.IssueState != 2) throw BadRequest("產品流程只能引用 ACTIVE 流程版本。");
        return fixedVersion;
    }

    private void ValidateDefault(string no, string version, int moType, string segment, bool requested, int? exceptIndex)
    {
        var group = _productProcesses.Select((x, i) => (x, i)).Where(y => y.x.ProductNo.Equals(no, StringComparison.OrdinalIgnoreCase) && y.x.ProductVersion.Equals(version, StringComparison.OrdinalIgnoreCase) && y.x.MoTypeNo == moType && y.x.ProductionSegmentNo.Equals(segment, StringComparison.OrdinalIgnoreCase) && y.x.DefaultProcess && y.i != exceptIndex).ToArray();
        if (requested && group.Length > 0) throw Conflict("同一產品、工單類型與製程段只能有一條預設流程。");
        if (!requested && group.Length == 0 && !_productProcesses.Any(x => x.ProductNo.Equals(no, StringComparison.OrdinalIgnoreCase) && x.ProductVersion.Equals(version, StringComparison.OrdinalIgnoreCase) && x.MoTypeNo == moType && x.ProductionSegmentNo.Equals(segment, StringComparison.OrdinalIgnoreCase) && !x.DefaultProcess))
            throw BadRequest("同一產品、工單類型與製程段至少需要一條預設流程。");
    }

    private static ProductProcessDto MakeProductProcess(ProductDto product, ProductProcessWriteRequest request, ProcessDto process, string selector) =>
        new(product.ProductNo, product.ProductVersion, request.MoTypeNo, Key(request.ProductionSegmentNo), SegmentOrder(request.ProductionSegmentNo), process.ProcessNo,
            selector, process.ProcessVersion, request.DefaultProcess, SegmentHasComponent(request.ProductionSegmentNo), SegmentHasLevel(request.ProductionSegmentNo));

    private static bool SameAssignment(ProductProcessDto x, string no, string version, int moType, string segment, string processNo, string selector) =>
        x.ProductNo.Equals(no, StringComparison.OrdinalIgnoreCase) && x.ProductVersion.Equals(version, StringComparison.OrdinalIgnoreCase) && x.MoTypeNo == moType &&
        x.ProductionSegmentNo.Equals(segment, StringComparison.OrdinalIgnoreCase) && x.ProcessNo.Equals(processNo, StringComparison.OrdinalIgnoreCase) &&
        x.ProcessVersionSelector.Equals(selector, StringComparison.OrdinalIgnoreCase);

    private static int SegmentOrder(string segment) => Key(segment).ToUpperInvariant() switch { "FRONT" => 10, "BACK" => 20, "ASSY" => 30, _ => 99 };
    private static bool SegmentHasComponent(string segment) => Key(segment).ToUpperInvariant() is "FRONT" or "ASSY";
    private static bool SegmentHasLevel(string segment) => Key(segment).ToUpperInvariant() is "ASSY";

    private OperationDto GetOperationUnsafe(string no) => _operations.TryGetValue(Key(no), out var x) ? x : throw NotFound("作業", no);
    private MaterialDto GetMaterialUnsafe(string no) => _materials.TryGetValue(Key(no), out var x) ? x : throw NotFound("物料", no);
    private ProcessDto GetProcessUnsafe(string no, string version) => _processes.TryGetValue((Key(no), Key(version)), out var x) ? x : throw NotFound("流程", $"{no}/{version}");
    private ProductDto GetProductUnsafe(string no, string version) => _products.TryGetValue((Key(no), Key(version)), out var x) ? x : throw NotFound("產品", $"{no}/{version}");

    private static OperationDto MapOperation(OperationWriteRequest x, int state, int stamp, DateTime created) => new()
    {
        OperationNo = Key(x.OperationNo), OperationName = x.OperationName.Trim(), OperationType = Key(x.OperationType), OperationClass = x.OperationClass,
        ShortName = x.ShortName.Trim(), OperationOrder = x.OperationOrder, ProductionSegmentNo = Key(x.ProductionSegmentNo), Description = x.Description.Trim(),
        MaterialOption = x.MaterialOption, MultiOperatorMode = x.MultiOperatorMode, PrintOutOnRunCard = x.PrintOutOnRunCard,
        SpcInspAbnormalHoldLot = x.SpcInspAbnormalHoldLot, ErrorQtyToSpc = x.ErrorQtyToSpc, StandardUnitRunTime = x.StandardUnitRunTime,
        CountOperationUnitQty = x.CountOperationUnitQty, StandardQueueTime = x.StandardQueueTime, IssueState = state, DataStamp = stamp, CreatedAt = created
    };

    private static MaterialDto MapMaterial(MaterialWriteRequest x, int state, int stamp, DateTime created) => new()
    {
        MaterialNo = Key(x.MaterialNo), MaterialType = Key(x.MaterialType), MaterialName = x.MaterialName.Trim(), MaterialSpec = x.MaterialSpec.Trim(),
        Description = x.Description.Trim(), PutInPlace = Key(x.PutInPlace), CountWay = x.CountWay, CheckLotNo = x.CheckLotNo, UnitType = Key(x.UnitType),
        UnitNo = Key(x.UnitNo), SafeQty = x.SafeQty, KeyMaterials = x.KeyMaterials, OpenLimitTimeFlag = x.OpenLimitTimeFlag,
        OpenLimitTime = x.OpenLimitTime, ResetOpenDate = x.ResetOpenDate, ExpireDateFlag = x.ExpireDateFlag, ErpNo = Key(x.ErpNo),
        IssueState = state, DataStamp = stamp, CreatedAt = created
    };

    private static ProcessDto MapProcess(ProcessWriteRequest x, int state, bool currentVersion, int stamp, DateTime created) => new()
    {
        ProcessNo = Key(x.ProcessNo), ProcessVersion = Key(x.ProcessVersion), ProcessType = Key(x.ProcessType),
        ProductionSegmentNo = Key(x.ProductionSegmentNo), ProcessClass = x.ProcessClass, Description = x.Description.Trim(),
        TuningProcess = x.TuningProcess, PartialType = x.PartialType, IssueState = state, CurrentVersion = currentVersion, DataStamp = stamp, CreatedAt = created
    };

    private static ProductDto MapProduct(ProductWriteRequest x, int state, bool currentVersion, int stamp, DateTime created) => new()
    {
        ProductNo = Key(x.ProductNo), ProductVersion = Key(x.ProductVersion), ProductName = x.ProductName.Trim(), ProductType = Key(x.ProductType),
        UnitType = Key(x.UnitType), UnitNo = Key(x.UnitNo), Description = x.Description.Trim(), SpecNo = Key(x.SpecNo), ProductCode = Key(x.ProductCode),
        SerialTypeNoLot = Key(x.SerialTypeNoLot), SerialTypeNoComp = Key(x.SerialTypeNoComp), LotStdQty = x.LotStdQty, ErpNo = Key(x.ErpNo),
        IssueState = state, CurrentVersion = currentVersion, DataStamp = stamp, CreatedAt = created
    };

    private void SeedOperation(string no, string name, string type, string segment, int state, DateTime now)
    {
        var request = new OperationWriteRequest { OperationNo = no, OperationName = name, OperationType = type, OperationClass = 1, ProductionSegmentNo = segment, OperationOrder = 10, CountOperationUnitQty = 1, StandardUnitRunTime = 2.5m, StandardQueueTime = 1m };
        _operations[Key(no)] = MapOperation(request, state, 1, now.AddDays(-30));
    }

    private void SeedMaterial(string no, string type, string name, string spec, string unitType, string unitNo, int state, DateTime now)
    {
        var request = new MaterialWriteRequest { MaterialNo = no, MaterialType = type, MaterialName = name, MaterialSpec = spec, UnitType = unitType, UnitNo = unitNo, PutInPlace = "OP", SafeQty = 50, CheckLotNo = 1, ExpireDateFlag = type == "CHEMICAL", OpenLimitTimeFlag = type == "CHEMICAL", OpenLimitTime = type == "CHEMICAL" ? 8 : null };
        _materials[Key(no)] = MapMaterial(request, state, 1, now.AddDays(-30));
    }

    private void SeedProcess(string no, string version, string type, string segment, int processClass, string description, bool current, int state, DateTime now, IReadOnlyList<ProcessNodeDto> nodes, IReadOnlyList<ProcessEdgeDto> edges)
    {
        var request = new ProcessWriteRequest { ProcessNo = no, ProcessVersion = version, ProcessType = type, ProductionSegmentNo = segment, ProcessClass = processClass, Description = description };
        var key = (Key(no), Key(version));
        _processes[key] = MapProcess(request, state, current, 1, now.AddDays(-30));
        _graphs[key] = new ProcessGraphDto(nodes, edges, 1);
    }

    private void SeedProduct(string no, string version, string name, string type, string unitType, string unitNo, decimal lotQty, bool current, int state, DateTime now)
    {
        var request = new ProductWriteRequest { ProductNo = no, ProductVersion = version, ProductName = name, ProductType = type, UnitType = unitType, UnitNo = unitNo, LotStdQty = lotQty };
        _products[(Key(no), Key(version))] = MapProduct(request, state, current, 1, now.AddDays(-30));
    }

    private static MasterDataRuleException NotFound(string entity, string id) => new(404, $"找不到{entity} {id}。");
    private static MasterDataRuleException Conflict(string message) => new(409, message);
    private static MasterDataRuleException BadRequest(string message) => new(400, message);
    private static string Key(string? value) => (value ?? "").Trim().ToUpperInvariant();
    private static bool ContainsAny(string? keyword, params string[] values) => string.IsNullOrWhiteSpace(keyword) || values.Any(x => x.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    private static void ValidateOperationNo(string no)
    {
        if (no.Contains(',')) throw BadRequest("OPNo 不可包含逗號。");
        if (no.Equals("LOTCREATE", StringComparison.OrdinalIgnoreCase)) throw BadRequest("LOTCREATE 是保留作業編號。");
    }

    private static void CheckStamp(int current, int expected)
    {
        if (current != expected) throw Conflict("資料已被其他人更新，請重新載入後再儲存。");
    }

    private static void CheckEditable(int state, int currentStamp, int expectedStamp)
    {
        if (state != 0) throw Conflict("只有 UNFROZEN 主檔可以編輯。");
        CheckStamp(currentStamp, expectedStamp);
    }

    private static void CheckRemoval(int currentState, int requestedState, int currentStamp, int expectedStamp)
    {
        if (currentState != requestedState) throw Conflict("主檔狀態已變更，請重新載入後再操作。");
        if (currentState == 1) throw Conflict("PENDING 主檔不可刪除。");
        CheckStamp(currentStamp, expectedStamp);
    }

    private T Read<T>(Func<T> action) { lock (_gate) return action(); }
    private T Write<T>(Func<T> action) { lock (_gate) return action(); }

}
