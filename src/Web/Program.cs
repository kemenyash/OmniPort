using BusinessLogic.Interfaces;
using Web;
using Web.Bootstrap;
using Web.Pages;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOmniPort(builder.Configuration);

WebApplication app = builder.Build();

await app.MigrateOmniPortDatabasesAsync();

app.UseOmniPortPipeline();

app.MapOmniPortAuthEndpoints();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.Services.GetRequiredService<IAppSyncContext>().Initialize();

app.Run();
