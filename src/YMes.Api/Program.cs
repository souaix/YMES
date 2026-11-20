using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using MES.Api.Data;
using Npgsql;
using YMes.Api.Data;
using YMes.Api.Endpoints;
using YMes.Api.Quality;
using YMes.Api.Security;

var builder = WebApplication.CreateBuilder(args);

var ymesEnvironment = builder.Configuration["YMES_ENVIRONMENT"] ?? builder.Environment.EnvironmentName;
var localDemoAuth = builder.Configuration.GetValue<bool>("YMES_LOCAL_DEMO_AUTH");
if (localDemoAuth)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
}
if (localDemoAuth &&
    (!builder.Environment.IsDevelopment() || string.Equals(ymesEnvironment, "production", StringComparison.OrdinalIgnoreCase)))
{
    throw new InvalidOperationException("YMES_LOCAL_DEMO_AUTH can only be enabled in a non-production Development environment.");
}

var connectionString = builder.Configuration.GetConnectionString("YMes") ??
                       builder.Configuration["YMES_DATABASE_URL"];
if (!localDemoAuth && string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Set ConnectionStrings__YMes or YMES_DATABASE_URL through the deployment secret store.");
}

var dataProtectionPath = builder.Configuration["YMES_DATA_PROTECTION_PATH"];
if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(dataProtectionPath))
{
    throw new InvalidOperationException(
        "Set YMES_DATA_PROTECTION_PATH to persistent, access-controlled storage outside the release directory.");
}

if (!string.IsNullOrWhiteSpace(dataProtectionPath))
{
    Directory.CreateDirectory(dataProtectionPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
}

if (!localDemoAuth)
{
    var dataSource = NpgsqlDataSource.Create(connectionString!);
    builder.Services.AddSingleton(dataSource);
    builder.Services.AddSingleton<AuthRepository>();
    builder.Services.AddSingleton<NavigationRepository>();
    builder.Services.AddSingleton<IYmesQc112FileStorage, LocalYmesQc112FileStorage>();
    builder.Services.AddSingleton<Qc112Repository>();
    builder.Services.AddSingleton<Qc201Repository>();
    builder.Services.AddSingleton<Qc202Repository>();
    builder.Services.AddSingleton<YMes.Api.Operations.Op105Repository>();
    builder.Services.AddSingleton<YMes.Api.Operations.OpB02Repository>();
    builder.Services.AddSingleton<YMes.Api.Operations.OpB03Repository>();
    builder.Services.AddSingleton<YMes.Api.Enterprise.Et102Repository>();
    builder.Services.AddSingleton<YMes.Api.Enterprise.Et103Repository>();
}
builder.Services.AddControllers();

var masterDataProvider = (builder.Configuration["YMES_MASTER_DATA_PROVIDER"] ??
                          (builder.Environment.IsDevelopment() ? "fake" : "disabled"))
    .Trim()
    .ToLowerInvariant();
var useFakeMasterData = masterDataProvider == "fake";
if (useFakeMasterData && string.Equals(ymesEnvironment, "production", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("The in-memory master-data provider is only allowed outside production.");
}
if (masterDataProvider is not ("fake" or "postgres" or "disabled"))
{
    throw new InvalidOperationException("YMES_MASTER_DATA_PROVIDER must be 'fake', 'postgres' or 'disabled'.");
}
if (useFakeMasterData)
{
    builder.Services.AddSingleton<IEquipmentRepository, FakeEquipmentRepository>();
    builder.Services.AddSingleton<IMasterDataRepository, FakeMasterDataRepository>();
}
else if (masterDataProvider == "postgres")
{
    builder.Services.AddSingleton<IEquipmentRepository, PostgresEquipmentRepository>();
    builder.Services.AddSingleton<PostgresEquipmentVendorRepository>();
    builder.Services.AddSingleton<PostgresEquipmentStateRepository>();
    builder.Services.AddSingleton<PostgresSmdRepository>();
    builder.Services.AddSingleton<IOperationRepository, PostgresOperationRepository>();
    builder.Services.AddSingleton<PostgresMasterDataRepository>();
    builder.Services.AddSingleton<IMasterDataRepository>(serviceProvider =>
        serviceProvider.GetRequiredService<PostgresMasterDataRepository>());
}
builder.Services.Configure<PasswordHasherOptions>(options =>
{
    options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
    options.IterationCount = 220_000;
});
builder.Services.AddSingleton<IPasswordHasher<AuthAccount>, PasswordHasher<AuthAccount>>();
builder.Services.AddSingleton(serviceProvider =>
{
    var hasher = serviceProvider.GetRequiredService<IPasswordHasher<AuthAccount>>();
    var dummyPassword = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    var hash = hasher.HashPassword(DummyPasswordHash.Account, dummyPassword);
    return new DummyPasswordHash(hash);
});

var allowedOrigins = (builder.Configuration["YMES_ALLOWED_ORIGINS"] ??
                      builder.Configuration["Security:AllowedOrigins"] ??
                      "http://localhost:5173")
    .Split(new[] { ',', ';' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Content-Type")
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
    {
        var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = builder.Environment.IsDevelopment()
            ? "ymes-auth"
            : "__Host-ymes-auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Path = "/";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

var apiPathBase = builder.Configuration["YMES_PATH_BASE"];
if (!string.IsNullOrWhiteSpace(apiPathBase))
{
    app.UsePathBase(apiPathBase);
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    if (context.Request.Path.StartsWithSegments("/auth") ||
        context.Request.Path.StartsWithSegments("/api/navigation"))
    {
        context.Response.Headers["Cache-Control"] = "no-store";
    }

    await next();
});

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
if (localDemoAuth)
{
    app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", mode = "local-demo" }));
    app.MapLocalDemoAuthEndpoints();
}
else
{
    app.MapGet("/health/ready", async (AuthRepository repository, CancellationToken cancellationToken) =>
    {
        try
        {
            return await repository.IsDatabaseReadyAsync(cancellationToken)
                ? Results.Ok(new { status = "ready" })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        catch
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    });

    app.MapAuthEndpoints();
    app.MapNavigationEndpoints();
}

if (useFakeMasterData)
{
    app.MapControllers().RequireAuthorization();
}
else if (masterDataProvider == "postgres")
{
    app.MapControllers().RequireAuthorization();
}

app.Run();
