using MES.Api.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<EquipmentRepository>();
builder.Services.AddSingleton<MasterDataRepository>();

var app = builder.Build();
app.MapGet("/", () => Results.Redirect("/api/equipment"));
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "MES Master Data API" }));
app.MapControllers();
app.Run();
