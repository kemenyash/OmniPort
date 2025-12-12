using OmniPort.Core.Interfaces;
using OmniPort.UI;
using OmniPort.UI.Bootstrap;
using OmniPort.UI.Pages;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOmniPort(builder.Configuration);

WebApplication app = builder.Build();

await app.MigrateOmniPortDatabasesAsync();

app.UseOmniPortPipeline();

app.MapOmniPortAuthEndpoints();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.Services.GetRequiredService<IAppSyncContext>().Initialize();

app.Run();
