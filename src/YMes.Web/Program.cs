using System.Net;
using MES.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CookieForwardingHandler>();

var dataProtectionPath = builder.Configuration["YMES_WEB_DATA_PROTECTION_PATH"];
if (!string.IsNullOrWhiteSpace(dataProtectionPath))
{
    Directory.CreateDirectory(dataProtectionPath);
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
}
else if (builder.Environment.IsDevelopment())
{
    var localKeyPath = Path.Combine(builder.Environment.ContentRootPath, ".data-protection-keys");
    Directory.CreateDirectory(localKeyPath);
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(localKeyPath));
}

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5080/";
void ConfigureApiClient(HttpClient client) =>
    client.BaseAddress = new Uri(apiBaseUrl.EndsWith('/') ? apiBaseUrl : $"{apiBaseUrl}/");

builder.Services.AddHttpClient<EquipmentApiClient>(ConfigureApiClient)
    .AddHttpMessageHandler<CookieForwardingHandler>();
builder.Services.AddHttpClient<MasterDataApiClient>(ConfigureApiClient)
    .AddHttpMessageHandler<CookieForwardingHandler>();
builder.Services.AddHttpClient("AuthApi", ConfigureApiClient)
    .AddHttpMessageHandler<CookieForwardingHandler>();

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

var pathBase = builder.Configuration["YMES_PATH_BASE"];
if (!string.IsNullOrWhiteSpace(pathBase))
{
    app.UsePathBase(pathBase);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.Use(async (context, next) =>
{
    var isMasterPage = context.Request.Path.StartsWithSegments("/Equipment") ||
                       context.Request.Path.StartsWithSegments("/MasterData");
    if (!isMasterPage)
    {
        await next();
        return;
    }

    try
    {
        var authApi = context.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("AuthApi");
        using var response = await authApi.GetAsync("auth/me", context.RequestAborted);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            context.Response.Redirect($"{context.Request.PathBase}/");
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }
    }
    catch (HttpRequestException)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return;
    }

    await next();
});

app.MapGet("/health/live", () => Results.Ok(new { status = "live", service = "YMES Web" }));
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
