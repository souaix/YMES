using MES.Contracts;

namespace MES.Api.Data;

public sealed class FakeEquipmentRepository : IEquipmentRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, EquipmentDto> _items = new(StringComparer.OrdinalIgnoreCase);

    public FakeEquipmentRepository()
    {
        var now = DateTime.UtcNow;
        AddSeed("EQ-ETCH-01", "ETCH", "Etch Chamber 01", "ENG-ETCH", "AMAT", "Producer 1", "AS-24001", "Production", 2, 2, 1, now);
        AddSeed("EQ-ETCH-02", "ETCH", "Etch Chamber 02", "ENG-ETCH", "AMAT", "Producer 1", "AS-24002", "Production", 2, 2, 1, now);
        AddSeed("EQ-CVD-01", "CVD", "CVD Furnace 01", "ENG-CVD", "TEL", "Trias", "AS-24018", "Production", 1, 1, 0, now);
        AddSeed("EQ-LITHO-01", "LITHO", "Lithography Scanner 01", "ENG-LITHO", "NIKON", "NSR-S635E", "AS-23071", "Production", 1, 2, 1, now);
        AddSeed("EQ-CLEAN-01", "CLEAN", "Wet Bench 01", "ENG-CLEAN", "SCREEN", "SU-3200", "AS-23106", "Production", 2, 1, 2, now);
        AddSeed("EQ-TEST-01", "TEST", "Electrical Tester 01", "ENG-TEST", "ADVANTEST", "V93000", "AS-22039", "Test", 4, 0, 0, now);
        AddSeed("EQ-ETCH-03", "ETCH", "Etch Chamber 03", "ENG-ETCH", "AMAT", "Producer 2", "AS-24103", "Production", 1, 2, 0, now);
        AddSeed("EQ-CVD-OLD", "CVD", "CVD Furnace Legacy", "ENG-CVD", "TEL", "Trias", "AS-19002", "Production", 1, 1, -1, now);

        AddChamber("CH-ETCH-01-A", "Etch Chamber 01 / A", "EQ-ETCH-01", now);
        AddChamber("CH-ETCH-01-B", "Etch Chamber 01 / B", "EQ-ETCH-01", now);
        AddChamber("CH-ETCH-02-A", "Etch Chamber 02 / A", "EQ-ETCH-02", now);
    }

    public IReadOnlyList<EquipmentDto> List(string? keyword, int? issueState, string? equipmentType)
    {
        lock (_gate)
        {
            return _items.Values
                .Where(x => !x.ChamberFlag)
                .Where(x => issueState is null || x.IssueState == issueState.Value)
                .Where(x => string.IsNullOrWhiteSpace(equipmentType) || x.EquipmentType.Equals(equipmentType, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.IsNullOrWhiteSpace(keyword) || x.EquipmentNo.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.EquipmentNo, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public EquipmentDto Get(string equipmentNo)
    {
        lock (_gate)
        {
            return _items.TryGetValue(equipmentNo, out var item)
                ? item
                : throw new EquipmentRuleException(404, $"找不到設備 {equipmentNo}。");
        }
    }

    public IReadOnlyList<EquipmentDto> Chambers(string equipmentNo)
    {
        lock (_gate)
        {
            if (!_items.ContainsKey(equipmentNo))
                throw new EquipmentRuleException(404, $"找不到設備 {equipmentNo}。");

            return _items.Values
                .Where(x => x.ChamberFlag && string.Equals(x.BelongEquipmentNo, equipmentNo, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.EquipmentNo, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public EquipmentDto CreateChamber(string parentEquipmentNo, ChamberWriteRequest request)
    {
        lock (_gate)
        {
            var parent = Find(parentEquipmentNo);
            EnsureChamberParent(parent);
            var no = request.EquipmentNo.Trim();
            if (string.IsNullOrWhiteSpace(no)) throw new EquipmentRuleException(400, "Chamber 編號不可空白。");
            if (_items.ContainsKey(no)) throw new EquipmentRuleException(409, $"設備編號 {no} 已存在。");
            var item = new EquipmentDto
            {
                EquipmentNo = no,
                EquipmentType = "CHAMBER",
                EquipmentName = request.EquipmentName.Trim(),
                EngineerGroupNo = parent.EngineerGroupNo,
                VendorNo = parent.VendorNo,
                Description = request.Description.Trim(),
                Capacity = 1,
                LoadPort = 0,
                ChamberFlag = true,
                BelongEquipmentNo = parent.EquipmentNo,
                IssueState = 0,
                DataStamp = 1,
                CreatedAt = DateTime.UtcNow
            };
            _items.Add(no, item);
            return item;
        }
    }

    public EquipmentDto UpdateChamber(string parentEquipmentNo, string chamberNo, ChamberWriteRequest request)
    {
        lock (_gate)
        {
            var parent = Find(parentEquipmentNo);
            EnsureChamberParent(parent);
            var current = Find(chamberNo);
            EnsureChamber(current, parentEquipmentNo);
            if (current.IssueState != 0) throw new EquipmentRuleException(409, "只有 UNFROZEN Chamber 可以編輯。");
            CheckStamp(current, request.DataStamp);
            if (!current.EquipmentNo.Equals(request.EquipmentNo.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new EquipmentRuleException(400, "更新時不可變更 Chamber 編號。");
            var updated = current with { EquipmentName = request.EquipmentName.Trim(), Description = request.Description.Trim(), DataStamp = current.DataStamp + 1, RevisedAt = DateTime.UtcNow };
            _items[chamberNo] = updated;
            return updated;
        }
    }

    public EquipmentDto ApproveChamber(string parentEquipmentNo, string chamberNo, int dataStamp)
    {
        lock (_gate)
        {
            var current = Find(chamberNo);
            EnsureChamber(current, parentEquipmentNo);
            CheckStamp(current, dataStamp);
            if (current.IssueState != 0) throw new EquipmentRuleException(409, "只有 UNFROZEN Chamber 可以核准。");
            var updated = current with { IssueState = 2, DataStamp = current.DataStamp + 1, RevisedAt = DateTime.UtcNow };
            _items[chamberNo] = updated;
            return updated;
        }
    }

    public EquipmentDto DeleteChamber(string parentEquipmentNo, string chamberNo, int issueState, int dataStamp)
    {
        lock (_gate)
        {
            var current = Find(chamberNo);
            EnsureChamber(current, parentEquipmentNo);
            CheckStamp(current, dataStamp);
            if (current.IssueState != issueState) throw new EquipmentRuleException(409, "Chamber 狀態已變更，請重新載入後再操作。");
            if (current.IssueState == 1) throw new EquipmentRuleException(409, "PENDING Chamber 不可刪除。");
            if (current.IssueState == 2)
            {
                var retired = current with { IssueState = -1, DataStamp = current.DataStamp + 1, RevisedAt = DateTime.UtcNow };
                _items[chamberNo] = retired;
                return retired;
            }
            _items.Remove(chamberNo);
            return current;
        }
    }

    public EquipmentDto Create(EquipmentWriteRequest request)
    {
        lock (_gate)
        {
            var equipmentNo = request.EquipmentNo.Trim();
            if (_items.ContainsKey(equipmentNo))
                throw new EquipmentRuleException(409, $"設備編號 {equipmentNo} 已存在。");

            var now = DateTime.UtcNow;
            var item = Map(request, issueState: 0, dataStamp: 1, createdAt: now, revisedAt: null);
            _items.Add(equipmentNo, item);
            return item;
        }
    }

    public EquipmentDto Update(string equipmentNo, EquipmentWriteRequest request)
    {
        lock (_gate)
        {
            var current = Find(equipmentNo);
            if (current.IssueState != 0)
                throw new EquipmentRuleException(409, "只有 UNFROZEN 設備可以編輯。");
            CheckStamp(current, request.DataStamp);

            var item = Map(request, current.IssueState, current.DataStamp + 1, current.CreatedAt, DateTime.UtcNow);
            _items[equipmentNo] = item;
            return item;
        }
    }

    public EquipmentDto Approve(string equipmentNo, int dataStamp)
    {
        lock (_gate)
        {
            var current = Find(equipmentNo);
            CheckStamp(current, dataStamp);
            if (current.IssueState != 0)
                throw new EquipmentRuleException(409, "只有 UNFROZEN 設備可以核准。");

            var updated = current with { IssueState = 2, DataStamp = current.DataStamp + 1, RevisedAt = DateTime.UtcNow };
            _items[equipmentNo] = updated;
            return updated;
        }
    }

    public EquipmentDto DeleteOrRetire(string equipmentNo, int issueState, int dataStamp)
    {
        lock (_gate)
        {
            var current = Find(equipmentNo);
            CheckStamp(current, dataStamp);
            if (current.IssueState != issueState)
                throw new EquipmentRuleException(409, "設備狀態已變更，請重新載入後再操作。");
            if (current.IssueState == 1)
                throw new EquipmentRuleException(409, "PENDING 設備不可刪除。");

            if (current.IssueState is 0 or -1)
            {
                if (_items.Values.Any(x => x.ChamberFlag && string.Equals(x.BelongEquipmentNo, equipmentNo, StringComparison.OrdinalIgnoreCase)))
                    throw new EquipmentRuleException(409, "設備仍有 Chamber，請先移除 Chamber。");
                _items.Remove(equipmentNo);
                return current;
            }

            if (_items.Values.Any(x => x.ChamberFlag && string.Equals(x.BelongEquipmentNo, equipmentNo, StringComparison.OrdinalIgnoreCase) && x.IssueState != -1))
                throw new EquipmentRuleException(409, "設備仍有使用中的 Chamber，請先停用或移除 Chamber。");

            var retired = current with { IssueState = -1, DataStamp = current.DataStamp + 1, RevisedAt = DateTime.UtcNow };
            _items[equipmentNo] = retired;
            return retired;
        }
    }

    private EquipmentDto Find(string equipmentNo) => _items.TryGetValue(equipmentNo, out var item)
        ? item
        : throw new EquipmentRuleException(404, $"找不到設備 {equipmentNo}。");

    private static void EnsureChamberParent(EquipmentDto parent)
    {
        if (parent.ChamberFlag) throw new EquipmentRuleException(400, "Chamber 不可再作為 Chamber 的父設備。");
        if (parent.IssueState is 1 or -1) throw new EquipmentRuleException(409, "PENDING 或 UNUSED 設備不可變更 Chamber。");
    }

    private static void EnsureChamber(EquipmentDto chamber, string parentEquipmentNo)
    {
        if (!chamber.ChamberFlag || !string.Equals(chamber.BelongEquipmentNo, parentEquipmentNo, StringComparison.OrdinalIgnoreCase))
            throw new EquipmentRuleException(404, $"設備 {chamber.EquipmentNo} 不屬於父設備 {parentEquipmentNo} 的 Chamber。");
    }

    private static void CheckStamp(EquipmentDto item, int dataStamp)
    {
        if (item.DataStamp != dataStamp)
            throw new EquipmentRuleException(409, "資料已由其他人更新，請重新載入後再儲存。");
    }

    private static EquipmentDto Map(EquipmentWriteRequest request, int issueState, int dataStamp, DateTime createdAt, DateTime? revisedAt) => new()
    {
        EquipmentNo = request.EquipmentNo.Trim(),
        EquipmentType = request.EquipmentType.Trim(),
        EquipmentName = request.EquipmentName.Trim(),
        EngineerGroupNo = request.EngineerGroupNo.Trim(),
        VendorNo = request.VendorNo.Trim(),
        ModelNo = request.ModelNo.Trim(),
        AssetNo = request.AssetNo.Trim(),
        EquipmentClass = request.EquipmentClass.Trim(),
        EquipmentClassNo = request.EquipmentClassNo.Trim(),
        Description = request.Description.Trim(),
        Capacity = request.Capacity,
        LoadPort = request.LoadPort,
        AutoFlag = request.AutoFlag,
        DefaultServer = request.DefaultServer,
        EAController = request.EAController.Trim(),
        EqpRecipe = request.EqpRecipe,
        QCListNo = request.QCListNo.Trim(),
        MaxTime = request.MaxTime,
        FixEqpTime = request.FixEqpTime,
        VarEqpTime = request.VarEqpTime,
        CountEqpUnitQty = request.CountEqpUnitQty,
        CountEquipmentUnitQty = request.CountEquipmentUnitQty,
        QueueServer = request.QueueServer.Trim(),
        RequestMsgQueue = request.RequestMsgQueue.Trim(),
        ResponseMsgQueue = request.ResponseMsgQueue.Trim(),
        RequestMsgQueueMes = request.RequestMsgQueueMes.Trim(),
        ResponseMsgQueueEap = request.ResponseMsgQueueEap.Trim(),
        IssueState = issueState,
        DataStamp = dataStamp,
        CreatedAt = createdAt,
        RevisedAt = revisedAt
    };

    private void AddSeed(string equipmentNo, string equipmentType, string equipmentName, string engineerGroupNo, string vendorNo,
        string modelNo, string assetNo, string equipmentClass, int capacity, int loadPort, int issueState, DateTime now)
    {
        _items[equipmentNo] = new EquipmentDto
        {
            EquipmentNo = equipmentNo,
            EquipmentType = equipmentType,
            EquipmentName = equipmentName,
            EngineerGroupNo = engineerGroupNo,
            VendorNo = vendorNo,
            ModelNo = modelNo,
            AssetNo = assetNo,
            EquipmentClass = equipmentClass,
            Capacity = capacity,
            LoadPort = loadPort,
            AutoFlag = issueState == 2 ? 1 : 0,
            DefaultServer = false,
            EAController = issueState == 2 ? "EAP" : "N/A",
            EqpRecipe = issueState == 2 ? 1 : 0,
            QCListNo = issueState == 2 ? "QC-EQP-01" : "",
            MaxTime = issueState == 2 ? 12 : null,
            FixEqpTime = issueState == 2 ? 0.5m : null,
            VarEqpTime = issueState == 2 ? 0.25m : null,
            CountEqpUnitQty = 1,
            CountEquipmentUnitQty = 1,
            IssueState = issueState,
            DataStamp = 1,
            CreatedAt = now.AddDays(-30)
        };
    }

    private void AddChamber(string equipmentNo, string equipmentName, string parentEquipmentNo, DateTime now)
    {
        _items[equipmentNo] = new EquipmentDto
        {
            EquipmentNo = equipmentNo,
            EquipmentType = "CHAMBER",
            EquipmentName = equipmentName,
            EngineerGroupNo = "ENG-ETCH",
            VendorNo = "N/A",
            Capacity = 1,
            LoadPort = 0,
            ChamberFlag = true,
            BelongEquipmentNo = parentEquipmentNo,
            IssueState = 2,
            DataStamp = 1,
            CreatedAt = now.AddDays(-30)
        };
    }
}
