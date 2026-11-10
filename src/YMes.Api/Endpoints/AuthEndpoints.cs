using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using YMes.Api.Data;
using YMes.Api.Security;

namespace YMes.Api.Endpoints;

public static partial class AuthEndpoints
{
    private const string InvalidLoginMessage = "帳號或密碼不正確，或帳號目前無法使用。";

    [GeneratedRegex("^[a-zA-Z0-9._-]{1,10}$", RegexOptions.CultureInvariant)]
    private static partial Regex UserNoPattern();

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", LoginAsync)
            .RequireRateLimiting("login");

        endpoints.MapGet("/auth/me", (ClaimsPrincipal principal) =>
        {
            var userNo = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userNo))
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new AuthenticatedUserResponse(
                userNo,
                principal.Identity?.Name ?? userNo,
                principal.FindFirstValue("must_change_password") == "true"));
        }).RequireAuthorization();

        endpoints.MapPost("/auth/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }).RequireAuthorization();

        endpoints.MapPost("/auth/change-password", ChangePasswordAsync)
            .RequireAuthorization()
            .RequireRateLimiting("login");

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext context,
        AuthRepository repository,
        IPasswordHasher<AuthAccount> passwordHasher,
        DummyPasswordHash dummyHash,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var userNo = request.Username?.Trim().ToLowerInvariant();
        var password = request.Password;
        if (string.IsNullOrWhiteSpace(userNo) ||
            !UserNoPattern().IsMatch(userNo) ||
            string.IsNullOrEmpty(password) ||
            password.Length > PasswordPolicy.MaximumLength * 2 ||
            PasswordPolicy.CountCodePoints(password) > PasswordPolicy.MaximumLength)
        {
            return Results.BadRequest(new { message = "請輸入有效帳號與密碼。" });
        }

        password = PasswordPolicy.NormalizeForHash(password);
        var account = await repository.FindByUserNoAsync(userNo, cancellationToken);
        var accountForVerification = account ?? DummyPasswordHash.Account;
        var hash = account?.PasswordHash ?? dummyHash.Value;
        PasswordVerificationResult verification;
        try
        {
            verification = passwordHasher.VerifyHashedPassword(accountForVerification, hash, password);
        }
        catch (FormatException)
        {
            passwordHasher.VerifyHashedPassword(DummyPasswordHash.Account, dummyHash.Value, password);
            verification = PasswordVerificationResult.Failed;
        }
        var isLocked = account?.LockedUntilUtc is { } lockedUntil && lockedUntil > DateTime.UtcNow;
        var authenticated = account is { IsActive: true } &&
                            !isLocked &&
                            verification != PasswordVerificationResult.Failed;

        var environment = configuration["YMES_ENVIRONMENT"] ?? context.RequestServices
            .GetRequiredService<IHostEnvironment>().EnvironmentName;
        await repository.WriteAuditEventAsync(
            account?.UserNo ?? userNo,
            authenticated ? "LOGIN_SUCCESS" : "LOGIN_FAILURE",
            authenticated,
            environment,
            context.Connection.RemoteIpAddress?.ToString(),
            Truncate(context.Request.Headers["User-Agent"].ToString(), 512),
            cancellationToken);

        if (!authenticated || account is null)
        {
            if (account is not null && !isLocked)
            {
                await repository.RecordLoginFailureAsync(account.UserNo, cancellationToken);
            }

            return Results.Json(
                new { message = InvalidLoginMessage },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var upgradedHash = passwordHasher.HashPassword(account, password);
            await repository.RehashPasswordAsync(account.UserNo, upgradedHash, cancellationToken);
        }

        await repository.RecordSuccessfulLoginAsync(account.UserNo, cancellationToken);

        var principal = CreatePrincipal(account);
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return Results.Ok(new AuthenticatedUserResponse(
            account.UserNo,
            account.DisplayName,
            account.MustChangePassword));
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        AuthRepository repository,
        IPasswordHasher<AuthAccount> passwordHasher,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var userNo = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentPassword = request.CurrentPassword;
        var newPassword = request.NewPassword;
        if (string.IsNullOrWhiteSpace(userNo))
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
        {
            return Results.BadRequest(new { message = "請輸入目前密碼和新密碼。" });
        }

        var policyError = PasswordPolicy.GetValidationError(newPassword, userNo);
        if (policyError is not null)
        {
            return Results.BadRequest(new { message = policyError });
        }

        var account = await repository.FindByUserNoAsync(userNo, cancellationToken);
        if (account is null || !account.IsActive ||
            passwordHasher.VerifyHashedPassword(
                account,
                account.PasswordHash,
                PasswordPolicy.NormalizeForHash(currentPassword)) ==
                PasswordVerificationResult.Failed)
        {
            return Results.Unauthorized();
        }

        var newHash = passwordHasher.HashPassword(
            account,
            PasswordPolicy.NormalizeForHash(newPassword));
        await repository.ChangePasswordAsync(account.UserNo, newHash, cancellationToken);
        await repository.WriteAuditEventAsync(
            account.UserNo,
            "PASSWORD_CHANGED",
            true,
            configuration["YMES_ENVIRONMENT"] ??
                context.RequestServices.GetRequiredService<IHostEnvironment>().EnvironmentName,
            context.Connection.RemoteIpAddress?.ToString(),
            Truncate(context.Request.Headers["User-Agent"].ToString(), 512),
            cancellationToken);

        var updatedAccount = account with { PasswordHash = newHash, MustChangePassword = false };
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CreatePrincipal(updatedAccount),
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return Results.NoContent();
    }

    private static ClaimsPrincipal CreatePrincipal(AuthAccount account)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.UserNo),
            new Claim(ClaimTypes.Name, account.DisplayName),
            new Claim("user_no", account.UserNo),
            new Claim("must_change_password", account.MustChangePassword ? "true" : "false")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];
}

public sealed record DummyPasswordHash(string Value)
{
    public static AuthAccount Account { get; } =
        new("unknown", "unknown", string.Empty, false, false, 0, null);
}
