using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;

namespace YMes.Api.Endpoints;

public static class LocalDemoAuthEndpoints
{
    public static IEndpointRouteBuilder MapLocalDemoAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", async (LocalDemoLoginRequest request, HttpContext context) =>
        {
            if (!string.Equals(request.Username?.Trim(), "ymes", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(request.Password, "1234", StringComparison.Ordinal))
            {
                return Results.Json(
                    new { message = "帳號或密碼不正確，或帳號目前無法使用。" },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "ymes"),
                new Claim(ClaimTypes.Name, "ymes"),
                new Claim("user_no", "ymes"),
                new Claim("must_change_password", "false")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme));

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            return Results.Ok(new { userNo = "ymes", displayName = "ymes", mustChangePassword = false });
        }).RequireRateLimiting("login");

        endpoints.MapGet("/auth/me", (ClaimsPrincipal principal) => Results.Ok(new
        {
            userNo = principal.FindFirstValue(ClaimTypes.NameIdentifier),
            displayName = principal.Identity?.Name ?? "ymes",
            mustChangePassword = false
        })).RequireAuthorization();

        endpoints.MapPost("/auth/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }).RequireAuthorization();

        return endpoints;
    }
}

public sealed record LocalDemoLoginRequest(string? Username, string? Password);
