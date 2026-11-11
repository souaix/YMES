using YMes.Api.Data;

namespace YMes.Api.Endpoints;

public static class NavigationEndpoints
{
    public static IEndpointRouteBuilder MapNavigationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/navigation/menu-tree", GetMenuTreeAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> GetMenuTreeAsync(
        HttpContext context,
        NavigationRepository repository,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await repository.GetMenuTreeAsync(cancellationToken));
    }
}
