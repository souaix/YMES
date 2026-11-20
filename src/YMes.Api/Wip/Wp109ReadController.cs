using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace YMes.Api.Wip;

[ApiController]
[Authorize]
[Route("api/wip/special-work-requests")]
public sealed class Wp109ReadController(Wp109ReadRepository repository,
    ILogger<Wp109ReadController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult List([FromQuery] Wp109Query query) => Execute(() => Ok(repository.List(query)));

    [HttpGet("item")]
    public IActionResult Item([FromQuery] string swrNo) => Execute(() => Ok(repository.Get(swrNo)));

    [HttpGet("attachment")]
    public async Task<IActionResult> Attachment([FromQuery] string swrNo, CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        try
        {
            await repository.StreamDownloadAsync(swrNo, async (metadata, content, token) =>
            {
                token.ThrowIfCancellationRequested();
                Response.ContentType = metadata.ContentType;
                Response.ContentLength = metadata.Length;
                Response.Headers["Content-Disposition"] = $"attachment; filename=\"{metadata.FileName}\"";
                Response.Headers["X-Content-Type-Options"] = "nosniff";
                // Fixed bounded transport buffer, not a business file-size cap.
                await content.CopyToAsync(Response.Body, 64 * 1024, token);
            }, cancellationToken);
            return new EmptyResult();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            HttpContext.Abort();
            return new EmptyResult();
        }
        catch (IOException)
        {
            // Never append a problem JSON body to an interrupted attachment.
            HttpContext.Abort();
            return new EmptyResult();
        }
        catch (Wp109ReadRuleException ex) when (!Response.HasStarted)
        { return DownloadProblem(ex.StatusCode, "特殊生產需求查詢失敗", ex.Message); }
        catch (NpgsqlException ex) when (!Response.HasStarted)
        {
            LogFailure(ex);
            return DownloadProblem(503, "特殊生產需求服務尚未就緒",
                "必要的SWR結構、讀取權限或附件來源尚未就緒；未使用替代檔案。");
        }
        catch (Exception ex) when (Response.HasStarted)
        {
            LogFailure(ex);
            HttpContext.Abort();
            return new EmptyResult();
        }
    }

    private IActionResult DownloadProblem(int status, string title, string detail)
    {
        // Headers may have been selected before the first stream read failed;
        // clear stale attachment length/type before returning an actual error.
        Response.Clear();
        Response.Headers["Cache-Control"] = "no-store";
        return Problem(statusCode: status, title: title, detail: detail);
    }

    private void LogFailure(Exception ex) => logger.LogWarning("WP109 attachment failed ({ErrorType}) for {Actor}",
        ex.GetType().Name, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "");

    private IActionResult Execute(Func<IActionResult> action)
    {
        Response.Headers["Cache-Control"] = "no-store";
        try { return action(); }
        catch (Wp109ReadRuleException ex)
        { return Problem(statusCode: ex.StatusCode, title: "特殊生產需求查詢失敗", detail: ex.Message); }
        catch (NpgsqlException ex)
        {
            logger.LogWarning("WP109 read failed ({ErrorType}) for {Actor}", ex.GetType().Name,
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "");
            return Problem(statusCode: 503, title: "特殊生產需求服務尚未就緒",
                detail: "必要的SWR結構或讀取權限尚未符合來源；未使用假資料或替代檔案。");
        }
    }
}
