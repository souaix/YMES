using System.Security.Claims;
using MES.Api.Data;
using MES.Api.Controllers;
using MES.Contracts;

namespace YMes.Api.Endpoints;

public static class EquipmentEndpoints
{
    public static IEndpointRouteBuilder MapPostgresEquipmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/equipment").RequireAuthorization();

        group.MapGet("", (string? keyword, int? issueState, string? equipmentType, IEquipmentRepository repository) =>
            Results.Ok(repository.List(keyword, issueState, equipmentType)));

        group.MapGet("/{equipmentNo}", (string equipmentNo, IEquipmentRepository repository) =>
            Execute(() => repository.Get(equipmentNo), "設備資料讀取失敗"));

        group.MapGet("/{equipmentNo}/chambers", (string equipmentNo, IEquipmentRepository repository) =>
            Execute(() => repository.Chambers(equipmentNo), "Chamber 資料讀取失敗"));

        group.MapPost("", (EquipmentWriteRequest request, HttpContext context, IEquipmentRepository repository) =>
        {
            try
            {
                var item = repository.Create(request, ActorUserNo(context));
                return Results.Created($"/api/equipment/{Uri.EscapeDataString(item.EquipmentNo)}", item);
            }
            catch (EquipmentRuleException ex) { return Problem(ex, "設備資料無法建立"); }
        });

        group.MapPut("/{equipmentNo}", (string equipmentNo, EquipmentWriteRequest request, HttpContext context, IEquipmentRepository repository) =>
        {
            if (!string.Equals(equipmentNo, request.EquipmentNo, StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { title = "設備編號不一致", detail = "網址與資料內容的 EquipmentNo 必須相同。" });
            return Execute(() => repository.Update(equipmentNo, request, ActorUserNo(context)), "設備資料無法更新");
        });

        group.MapPost("/{equipmentNo}/approve", (string equipmentNo, EquipmentCommandRequest request, HttpContext context, IEquipmentRepository repository) =>
            Execute(() => repository.Approve(equipmentNo, request.DataStamp, ActorUserNo(context)), "設備無法核准"));

        group.MapDelete("/{equipmentNo}", (string equipmentNo, int issueState, long dataStamp, HttpContext context, IEquipmentRepository repository) =>
            Execute(() => repository.DeleteOrRetire(equipmentNo, issueState, dataStamp, ActorUserNo(context)), "設備無法刪除或停用"));

        group.MapPost("/{equipmentNo}/chambers", (string equipmentNo, ChamberWriteRequest request, HttpContext context, IEquipmentRepository repository) =>
        {
            try
            {
                var item = repository.CreateChamber(equipmentNo, request, ActorUserNo(context));
                return Results.Created($"/api/equipment/{Uri.EscapeDataString(equipmentNo)}/chambers/{Uri.EscapeDataString(item.EquipmentNo)}", item);
            }
            catch (EquipmentRuleException ex) { return Problem(ex, "Chamber 無法建立"); }
        });

        group.MapPut("/{equipmentNo}/chambers/{chamberNo}", (string equipmentNo, string chamberNo, ChamberWriteRequest request, HttpContext context, IEquipmentRepository repository) =>
            Execute(() => repository.UpdateChamber(equipmentNo, chamberNo, request, ActorUserNo(context)), "Chamber 無法更新"));

        group.MapPost("/{equipmentNo}/chambers/{chamberNo}/approve", (string equipmentNo, string chamberNo, EquipmentCommandRequest request, HttpContext context, IEquipmentRepository repository) =>
            Execute(() => repository.ApproveChamber(equipmentNo, chamberNo, request.DataStamp, ActorUserNo(context)), "Chamber 無法核准"));

        group.MapDelete("/{equipmentNo}/chambers/{chamberNo}", (string equipmentNo, string chamberNo, int issueState, long dataStamp, HttpContext context, IEquipmentRepository repository) =>
            Execute(() => repository.DeleteChamber(equipmentNo, chamberNo, issueState, dataStamp, ActorUserNo(context)), "Chamber 無法刪除或停用"));

        return endpoints;
    }

    private static IResult Execute<T>(Func<T> action, string title)
    {
        try { return Results.Ok(action()); }
        catch (EquipmentRuleException ex)
        {
            return Problem(ex, title);
        }
    }

    private static IResult Problem(EquipmentRuleException exception, string title) =>
        Results.Problem(statusCode: exception.StatusCode, title: title, detail: exception.Message);

    private static string ActorUserNo(HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.Identity?.Name ?? "ymes";
}
