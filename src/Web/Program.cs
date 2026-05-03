using BusinessLogic.Interfaces;
using Web;
using Web.Bootstrap;
using Web.Pages;
using Web.Telemetry;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOmniPort(builder.Configuration);
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "HH:mm:ss";
    options.UseUtcTimestamp = true;
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
    {
        Indented = false
    };
});

WebApplication app = builder.Build();
ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("OmniPort starting in {Environment}", app.Environment.EnvironmentName);
await app.MigrateOmniPortDatabasesAsync();

app.UseOmniPortPipeline();

app.MapOmniPortAuthEndpoints();
app.MapOmniPortCultureEndpoints();
app.MapOmniPortWatchEndpoints();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.Services.GetRequiredService<IAppSyncContext>().Initialize();
logger.LogInformation("OmniPort initialized");

app.Run();
