using MES.Contracts;
using MES.Web.Models;
using MES.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace MES.Web.Controllers;

public sealed class EquipmentController(EquipmentApiClient api, MasterDataApiClient masterApi) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, int? issueState, string? selectedEquipmentNo, CancellationToken cancellationToken)
    {
        try
        {
            var all = await api.ListAsync(cancellationToken: cancellationToken);
            var state = issueState ?? 0;
            var items = await api.ListAsync(keyword, state, "EquipmentNo", cancellationToken);
            var selected = items.FirstOrDefault(x => string.Equals(x.EquipmentNo, selectedEquipmentNo, StringComparison.OrdinalIgnoreCase))
                ?? items.FirstOrDefault();
            IReadOnlyList<EquipmentDto> chambers = selected is null ? [] : await api.ChambersAsync(selected.EquipmentNo, cancellationToken);
            return View(new EquipmentIndexViewModel
            {
                Equipment = items,
                AllEquipment = all,
                Chambers = chambers,
                Selected = selected,
                Keyword = keyword,
                IssueState = state
            });
        }
        catch (HttpRequestException)
        {
            return View("ApiUnavailable");
        }
    }

    [HttpGet]
    public async Task<IActionResult> New(CancellationToken cancellationToken)
    {
        await LoadEquipmentLookups(cancellationToken);
        return View("Form", new EquipmentWriteRequest
    {
        Capacity = -1,
        LoadPort = -1,
        VendorNo = "N/A",
        EAController = "N/A",
        CountEqpUnitQty = 1,
        CountEquipmentUnitQty = 1
    });
    }


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> New(EquipmentWriteRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { await LoadEquipmentLookups(cancellationToken); return View("Form", request); }
        try
        {
            await api.CreateAsync(request, cancellationToken);
            TempData["Notice"] = $"設備 {request.EquipmentNo} 已建立為 UNFROZEN。";
            return RedirectToAction(nameof(Index), new { selectedEquipmentNo = request.EquipmentNo });
        }
        catch (HttpRequestException ex)
        {
            ModelState.AddModelError(string.Empty, ReadApiError(ex, "設備建立失敗；請確認編號是否重複。"));
            await LoadEquipmentLookups(cancellationToken);
            return View("Form", request);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
    {
        var item = await api.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        if (item.IssueState != 0) return Conflict("只有 UNFROZEN 設備可以編輯。");
        await LoadEquipmentLookups(cancellationToken);
        ViewData["CreatedAt"] = item.CreatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
        ViewData["RevisedAt"] = item.RevisedAt?.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss") ?? "";
        return View("Form", ToRequest(item));
    }

    [HttpGet]
    public async Task<IActionResult> Copy(string id, CancellationToken cancellationToken)
    {
        var item = await api.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        await LoadEquipmentLookups(cancellationToken);
        var request = ToRequest(item);
        request.EquipmentNo = "";
        request.DataStamp = 0;
        ViewData["IsCopy"] = true;
        return View("Form", request);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EquipmentWriteRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { await LoadEquipmentLookups(cancellationToken); return View("Form", request); }
        try
        {
            await api.UpdateAsync(id, request, cancellationToken);
            TempData["Notice"] = $"設備 {request.EquipmentNo} 已更新。";
            return RedirectToAction(nameof(Index), new { selectedEquipmentNo = request.EquipmentNo });
        }
        catch (HttpRequestException ex)
        {
            ModelState.AddModelError(string.Empty, ReadApiError(ex, "設備更新失敗；資料可能已被其他人更新。"));
            await LoadEquipmentLookups(cancellationToken);
            return View("Form", request);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string equipmentNo, CancellationToken cancellationToken)
    {
        var item = await api.GetAsync(equipmentNo, cancellationToken);
        if (item is null) return NotFound();
        try
        {
            await api.ApproveAsync(item, cancellationToken);
            TempData["Notice"] = $"設備 {equipmentNo} 已核准為 ACTIVE。";
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = ReadApiError(ex, "設備核准失敗。");
        }
        return RedirectToAction(nameof(Index), new { selectedEquipmentNo = equipmentNo });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string equipmentNo, CancellationToken cancellationToken)
    {
        var item = await api.GetAsync(equipmentNo, cancellationToken);
        if (item is null) return NotFound();
        try
        {
            await api.DeleteAsync(item, cancellationToken);
            TempData["Notice"] = item.IssueState == 2
                ? $"設備 {equipmentNo} 已停用並標記為 UNUSED。"
                : $"設備 {equipmentNo} 已刪除。";
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = ReadApiError(ex, "設備刪除或停用失敗。");
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> NewChamber(string parentEquipmentNo, CancellationToken cancellationToken)
    {
        var parent = await api.GetAsync(parentEquipmentNo, cancellationToken);
        if (parent is null) return NotFound();
        if (parent.ChamberFlag || parent.IssueState is 1 or -1) return Conflict("只有 UNFROZEN 或 ACTIVE 的父設備可以新增 Chamber。");
        return View("ChamberForm", new ChamberWriteRequest { BelongEquipmentNo = parentEquipmentNo });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NewChamber(string parentEquipmentNo, ChamberWriteRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("ChamberForm", request);
        try
        {
            await masterApi.PostAsync<ChamberWriteRequest, EquipmentDto>($"api/equipment/{Uri.EscapeDataString(parentEquipmentNo)}/chambers", request, cancellationToken);
            TempData["Notice"] = $"Chamber {request.EquipmentNo} 已建立為 UNFROZEN。";
            return RedirectToAction(nameof(Index), new { selectedEquipmentNo = parentEquipmentNo });
        }
        catch (HttpRequestException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View("ChamberForm", request); }
    }

    [HttpGet]
    public async Task<IActionResult> EditChamber(string parentEquipmentNo, string chamberNo, CancellationToken cancellationToken)
    {
        var chamber = await masterApi.GetAsync<EquipmentDto>($"api/equipment/{Uri.EscapeDataString(chamberNo)}", cancellationToken);
        if (chamber is null) return NotFound();
        if (!chamber.ChamberFlag || !string.Equals(chamber.BelongEquipmentNo, parentEquipmentNo, StringComparison.OrdinalIgnoreCase)) return NotFound();
        if (chamber.IssueState != 0) return Conflict("只有 UNFROZEN Chamber 可以編輯。");
        return View("ChamberForm", new ChamberWriteRequest { EquipmentNo = chamber.EquipmentNo, BelongEquipmentNo = parentEquipmentNo, EquipmentName = chamber.EquipmentName, Description = chamber.Description, DataStamp = chamber.DataStamp });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditChamber(string parentEquipmentNo, string chamberNo, ChamberWriteRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("ChamberForm", request);
        try
        {
            await masterApi.PutAsync<ChamberWriteRequest, EquipmentDto>($"api/equipment/{Uri.EscapeDataString(parentEquipmentNo)}/chambers/{Uri.EscapeDataString(chamberNo)}", request, cancellationToken);
            TempData["Notice"] = $"Chamber {chamberNo} 已更新。";
            return RedirectToAction(nameof(Index), new { selectedEquipmentNo = parentEquipmentNo });
        }
        catch (HttpRequestException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View("ChamberForm", request); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveChamber(string parentEquipmentNo, string chamberNo, int stamp, CancellationToken cancellationToken)
    {
        try { await masterApi.PostAsync<object, EquipmentDto>($"api/equipment/{Uri.EscapeDataString(parentEquipmentNo)}/chambers/{Uri.EscapeDataString(chamberNo)}/approve", new { DataStamp = stamp }, cancellationToken); TempData["Notice"] = $"Chamber {chamberNo} 已核准。"; }
        catch (HttpRequestException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index), new { selectedEquipmentNo = parentEquipmentNo });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChamber(string parentEquipmentNo, string chamberNo, int state, int stamp, CancellationToken cancellationToken)
    {
        try
        {
            await masterApi.DeleteAsync($"api/equipment/{Uri.EscapeDataString(parentEquipmentNo)}/chambers/{Uri.EscapeDataString(chamberNo)}?issueState={state}&dataStamp={stamp}", cancellationToken);
            TempData["Notice"] = state == 2 ? $"Chamber {chamberNo} 已停用。" : $"Chamber {chamberNo} 已刪除。";
        }
        catch (HttpRequestException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index), new { selectedEquipmentNo = parentEquipmentNo });
    }

    private async Task LoadEquipmentLookups(CancellationToken cancellationToken)
    {
        try
        {
            var items = await api.ListAsync(cancellationToken: cancellationToken);
            ViewData["EquipmentTypes"] = items.Select(x => x.EquipmentType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
            ViewData["EquipmentClasses"] = items.Select(x => x.EquipmentClass).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
            ViewData["EngineerGroups"] = items.Select(x => x.EngineerGroupNo).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
            ViewData["Vendors"] = items.Select(x => x.VendorNo).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
            ViewData["QcLists"] = items.Select(x => x.QCListNo).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        }
        catch (HttpRequestException) { }
    }

    private static EquipmentWriteRequest ToRequest(EquipmentDto item) => new()
    {
        EquipmentNo = item.EquipmentNo,
        EquipmentType = item.EquipmentType,
        EquipmentName = item.EquipmentName,
        EngineerGroupNo = item.EngineerGroupNo,
        VendorNo = item.VendorNo,
        ModelNo = item.ModelNo,
        AssetNo = item.AssetNo,
        EquipmentClass = item.EquipmentClass,
        EquipmentClassNo = item.EquipmentClassNo,
        Description = item.Description,
        Capacity = item.Capacity,
        LoadPort = item.LoadPort,
        AutoFlag = item.AutoFlag,
        DefaultServer = item.DefaultServer,
        EAController = item.EAController,
        EqpRecipe = item.EqpRecipe,
        QCListNo = item.QCListNo,
        MaxTime = item.MaxTime,
        FixEqpTime = item.FixEqpTime,
        VarEqpTime = item.VarEqpTime,
        CountEqpUnitQty = item.CountEqpUnitQty,
        CountEquipmentUnitQty = item.CountEquipmentUnitQty,
        QueueServer = item.QueueServer,
        RequestMsgQueue = item.RequestMsgQueue,
        ResponseMsgQueue = item.ResponseMsgQueue,
        RequestMsgQueueMes = item.RequestMsgQueueMes,
        ResponseMsgQueueEap = item.ResponseMsgQueueEap,
        DataStamp = item.DataStamp
    };

    private static string ReadApiError(HttpRequestException exception, string fallback) =>
        exception.StatusCode is null ? fallback : $"{fallback} (API HTTP {(int)exception.StatusCode.Value})";
}
