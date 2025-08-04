using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OmniPort.Core.Interfaces;
using OmniPort.Data;
using OmniPort.UI;
using OmniPort.UI.Components;
using OmniPort.UI.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<OmniPortDataContext>(options =>
    options.UseSqlite("Data Source=omniport.db"));
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<OmniPortMappingProfile>();
});



builder.Services.AddScoped<ITemplateService, TemplateService>();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

app.Run();
