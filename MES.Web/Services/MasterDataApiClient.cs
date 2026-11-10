using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MES.Web.Services;

public sealed class MasterDataApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<T>> GetListAsync<T>(string path, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<T>>(path, cancellationToken) ?? [];

    public Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<T>(path, cancellationToken);

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(path, body, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
            ?? throw new HttpRequestException("API 沒有回傳資料。");
    }

    public async Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(path, body, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
            ?? throw new HttpRequestException("API 沒有回傳資料。");
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var detail = $"API HTTP {(int)response.StatusCode}";
        try
        {
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            if (document.RootElement.TryGetProperty("detail", out var detailElement) && detailElement.ValueKind == JsonValueKind.String)
                detail = detailElement.GetString() ?? detail;
            else if (document.RootElement.TryGetProperty("title", out var titleElement) && titleElement.ValueKind == JsonValueKind.String)
                detail = titleElement.GetString() ?? detail;
        }
        catch (JsonException) { }
        throw new HttpRequestException(detail, null, response.StatusCode);
    }
}
