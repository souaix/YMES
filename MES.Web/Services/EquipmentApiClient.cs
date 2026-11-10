using System.Net.Http.Json;
using MES.Contracts;

namespace MES.Web.Services;

public sealed class EquipmentApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<EquipmentDto>> ListAsync(string? keyword = null, int? issueState = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword)) query.Add($"keyword={Uri.EscapeDataString(keyword)}");
        if (issueState.HasValue) query.Add($"issueState={issueState.Value}");
        var suffix = query.Count == 0 ? "" : $"?{string.Join("&", query)}";
        return await httpClient.GetFromJsonAsync<List<EquipmentDto>>($"api/equipment{suffix}", cancellationToken) ?? [];
    }

    public Task<EquipmentDto?> GetAsync(string equipmentNo, CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<EquipmentDto>($"api/equipment/{Uri.EscapeDataString(equipmentNo)}", cancellationToken);

    public async Task<IReadOnlyList<EquipmentDto>> ChambersAsync(string equipmentNo, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<EquipmentDto>>($"api/equipment/{Uri.EscapeDataString(equipmentNo)}/chambers", cancellationToken) ?? [];

    public async Task CreateAsync(EquipmentWriteRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/equipment", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateAsync(string equipmentNo, EquipmentWriteRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"api/equipment/{Uri.EscapeDataString(equipmentNo)}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ApproveAsync(EquipmentDto equipment, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync($"api/equipment/{Uri.EscapeDataString(equipment.EquipmentNo)}/approve",
            new EquipmentCommandRequest(equipment.DataStamp), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(EquipmentDto equipment, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(
            $"api/equipment/{Uri.EscapeDataString(equipment.EquipmentNo)}?issueState={equipment.IssueState}&dataStamp={equipment.DataStamp}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public sealed record EquipmentCommandRequest(int DataStamp);
