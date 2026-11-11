using System.Security.Claims;
using MES.Api.Data;
using MES.Contracts;

namespace YMes.Api.Endpoints;

public static class OperationEndpoints
{
    public static IEndpointRouteBuilder MapPostgresOperationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/operations").RequireAuthorization();
        group.MapGet("", (string? keyword, int? issueState, IOperationRepository repository) =>
            Results.Ok(repository.List(keyword, issueState)));
        group.MapGet("/{operationNo}", (string operationNo, IOperationRepository repository) =>
            Execute(() => repository.Get(operationNo), "作業資料讀取失敗"));
        group.MapPost("", (OperationWriteRequest request, HttpContext context, IOperationRepository repository) =>
        {
            try
            {
                var item = repository.Create(request, ActorUserNo(context));
                return Results.Created($"/api/operations/{Uri.EscapeDataString(item.OperationNo)}", item);
            }
            catch (MasterDataRuleException ex) { return Problem(ex, "作業資料無法建立"); }
        });
        group.MapPut("/{operationNo}", (string operationNo, OperationWriteRequest request, HttpContext context, IOperationRepository repository) =>
            Execute(() => repository.Update(operationNo, request, ActorUserNo(context)), "作業資料無法更新"));
        group.MapPost("/{operationNo}/approve", (string operationNo, OperationCommandRequest request, HttpContext context, IOperationRepository repository) =>
            Execute(() => repository.Approve(operationNo, request.DataStamp, ActorUserNo(context)), "作業無法核准"));
        group.MapDelete("/{operationNo}", (string operationNo, int issueState, long dataStamp, HttpContext context, IOperationRepository repository) =>
        {
            try
            {
                repository.DeleteOrRetire(operationNo, issueState, dataStamp, ActorUserNo(context));
                return Results.NoContent();
            }
            catch (MasterDataRuleException ex) { return Problem(ex, "作業無法刪除或停用"); }
        });
        return endpoints;
    }

    private static IResult Execute<T>(Func<T> action, string title)
    {
        try { return Results.Ok(action()); }
        catch (MasterDataRuleException ex) { return Problem(ex, title); }
    }

    private static IResult Problem(MasterDataRuleException exception, string title) =>
        Results.Problem(statusCode: exception.StatusCode, title: title, detail: exception.Message);

    private static string ActorUserNo(HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.Identity?.Name ?? "ymes";
}

public sealed record OperationCommandRequest(long DataStamp);
