using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/special-work-requests")]
public sealed class Wp109WriteController(Wp109WriteRepository repository, Wp109DeviceLookup devices,
    IOptions<JsonOptions> jsonOptions, ILogger<Wp109WriteController> logger) : ControllerBase
{
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

    [HttpGet("versioned-item")]
    public Task<IActionResult> VersionedItem([FromQuery] string swrNo, CancellationToken token) =>
        Execute(() => repository.VersionedItemAsync(swrNo, token));

    [HttpGet("number-preview")]
    public Task<IActionResult> Preview() => Execute(() => Task.FromResult(repository.Preview()));

    [HttpGet("writer-options")]
    public Task<IActionResult> Options(CancellationToken token) => Execute(() => repository.OptionsAsync(token));

    [HttpGet("devices")]
    public Task<IActionResult> Devices([FromQuery] string? keyword = null, [FromQuery] int limit = 200) =>
        Execute(() => Task.FromResult(devices.List(keyword, limit)));

    [HttpPost]
    [DisableRequestSizeLimit]
    public Task<IActionResult> Create(CancellationToken token) => Execute(async () =>
    {
        var parsed = await ReadCommand<Wp109CreateRequest>(token);
        await using var file = parsed.File;
        return await repository.CreateAsync(parsed.Request, file, Actor, token);
    });

    [HttpPut("item")]
    [DisableRequestSizeLimit]
    public Task<IActionResult> Edit([FromQuery] string swrNo, CancellationToken token) => Execute(async () =>
    {
        var parsed = await ReadCommand<Wp109EditRequest>(token);
        await using var file = parsed.File;
        return await repository.EditAsync(swrNo, parsed.Request, file, Actor, token);
    });

    [HttpPost("approve")]
    public Task<IActionResult> Approve([FromQuery] string swrNo,
        [FromBody] Wp109ExpectedVersionRequest request, CancellationToken token) =>
        Execute(() => repository.ApproveAsync(swrNo, request, Actor, token));

    [HttpPost("void")]
    public Task<IActionResult> Void([FromQuery] string swrNo,
        [FromBody] Wp109VoidRequest request, CancellationToken token) =>
        Execute(() => repository.VoidAsync(swrNo, request, Actor, token));

    [HttpDelete("item")]
    public Task<IActionResult> Delete([FromQuery] string swrNo,
        [FromBody] Wp109ExpectedVersionRequest request, CancellationToken token) =>
        Execute(() => repository.DeleteAsync(swrNo, request, Actor, token));

    private async Task<(T Request, Wp109IncomingAttachment? File)> ReadCommand<T>(CancellationToken token)
        where T : Wp109FieldsRequest
    {
        // Native bounded fields + an empty Lot array fit well under 8KiB.
        // This is an HTTP metadata resource bound, not a native column size.
        if (Request.HasJsonContentType()) return (await Metadata<T>(Request.Body, token), null);
        if (!MediaTypeHeaderValue.TryParse(Request.ContentType, out var media)
            || !media.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            throw new Wp109WriteRuleException(400, "請使用JSON或multipart/form-data。");
        var boundary = HeaderUtilities.RemoveQuotes(media.Boundary).Value;
        if (string.IsNullOrEmpty(boundary) || boundary.Length > 128)
            throw new Wp109WriteRuleException(400, "附件傳輸邊界無效。");
        var reader = new MultipartReader(boundary, Request.Body);
        var first = await reader.ReadNextSectionAsync(token);
        if (first is null || PartName(first) != "metadata")
            throw new Wp109WriteRuleException(400, "第一段須為metadata JSON。");
        var request = await Metadata<T>(first.Body, token);
        Wp109IncomingAttachment? file = null;
        try
        {
            var section = await reader.ReadNextSectionAsync(token);
            if (section is not null)
            {
                if (PartName(section) != "attachment") throw new Wp109WriteRuleException(400, "未知的附件欄位。");
                var limit = await repository.FileLimitAsync(token);
                file = await Wp109IncomingAttachment.ReadAsync(section.Body, limit, token);
                if (await reader.ReadNextSectionAsync(token) is not null)
                    throw new Wp109WriteRuleException(400, "只接受一個附件。");
            }
            return (request, file);
        }
        catch { if (file is not null) await file.DisposeAsync(); throw; }
    }

    private static string PartName(MultipartSection section)
    {
        if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var value)
            || !value.DispositionType.Equals("form-data", StringComparison.OrdinalIgnoreCase))
            throw new Wp109WriteRuleException(400, "附件欄位格式無效。");
        return HeaderUtilities.RemoveQuotes(value.Name).Value ?? "";
    }

    private async Task<T> Metadata<T>(Stream source, CancellationToken token) where T : Wp109FieldsRequest
    {
        using var bytes = new MemoryStream(); var buffer = new byte[1024]; int read;
        while ((read = await source.ReadAsync(buffer, token)) != 0)
        {
            if (bytes.Length + read > 8192) throw new Wp109WriteRuleException(400, "表單資料過大。");
            bytes.Write(buffer, 0, read);
        }
        return JsonSerializer.Deserialize<T>(bytes.GetBuffer().AsSpan(0, (int)bytes.Length),
            jsonOptions.Value.JsonSerializerOptions) ?? throw new Wp109WriteRuleException(400, "表單不可為NULL。");
    }

    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        Response.Headers.CacheControl = "no-store";
        try { return Ok(await action()); }
        catch (Wp109WriteRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "特殊生產需求操作失敗", detail: ex.Message); }
        catch (JsonException)
        { return Problem(statusCode: 400, title: "表單格式無效", detail: "必要欄位遺漏或型別不符。"); }
        catch (InvalidDataException)
        { return Problem(statusCode: 400, title: "附件傳輸格式無效"); }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Problem(statusCode: 409, title: "流水號或單號重複", detail: "本次未建立；請重新查詢核對。"); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        { HttpContext.Abort(); return new EmptyResult(); }
        catch (NpgsqlException ex)
        {
            Failure(ex); return Problem(statusCode: 503, title: "目前無法確認處理結果",
                detail: "請重新查詢核對實際結果，勿重複送出。");
        }
        catch (IOException ex)
        { Failure(ex); return Problem(statusCode: 503, title: "附件暫存未完成", detail: "請重新查詢核對，勿自動重送。"); }
    }

    private void Failure(Exception ex) => logger.LogWarning("WP109 write failed ({ErrorType}) for {Actor}",
        ex.GetType().Name, Actor);
}
