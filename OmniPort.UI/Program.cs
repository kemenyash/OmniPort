using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OmniPort.Core.Interfaces;
using OmniPort.Data;
using OmniPort.UI;
using OmniPort.UI.Components;
using OmniPort.UI.Presentation;
using OmniPort.UI.Presentation.Mapping;
using OmniPort.UI.Presentation.Services;
using OmniPort.UI.Presentation.ViewModels;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<OmniPortDataContext>(options =>
    options.UseSqlite("Data Source=omniport.db"));
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<OmniPortMappingProfile>();
});

builder.Services.Configure<UploadLimits>(builder.Configuration.GetSection("UploadLimits"));


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient();

builder.Services.AddScoped<ITemplateManager, TemplateManager>();
builder.Services.AddScoped<ITransformationManager, TransformationManager>();
builder.Services.AddScoped<ITransformationExecutionService, TransformationExecutor>();

builder.Services.AddScoped<TemplateEditorViewModel>();
builder.Services.AddScoped<JoinTemplatesViewModel>();
builder.Services.AddScoped<TransformationViewModel>();
builder.Services.AddSingleton<IAppSyncContext, AppSyncContext>();
builder.Services.AddSingleton<ISourceFingerprintStore, InMemorySourceFingerprintStore>();
builder.Services.AddHostedService<WatchedHashSyncService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OmniPortDataContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
await app.Services.GetRequiredService<IAppSyncContext>().InitializeAsync();
app.Run();
