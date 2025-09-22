using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniPort.Core.Interfaces;
using OmniPort.Data;
using OmniPort.Data.Auth;
using OmniPort.UI;
using OmniPort.UI.Components;
using OmniPort.UI.Presentation;
using OmniPort.UI.Presentation.Mapping;
using OmniPort.UI.Presentation.Services;
using OmniPort.UI.Presentation.ViewModels;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OmniPortDataContext>(options =>
    options.UseSqlite("Data Source=omniport.db"));

builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlite("Data Source=omniport.db"));

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<OmniPortMappingProfile>();
});

builder.Services.Configure<UploadLimits>(builder.Configuration.GetSection("UploadLimits"));

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

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
    options.ReturnUrlParameter = "returnUrl";
});

builder.Services.AddAuthorization();

builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppIdentityDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OmniPortDataContext>();
    dbContext.Database.Migrate();

    var identityDb = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
    identityDb.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/login", async (HttpContext http,
                                  SignInManager<AppUser> signInManager,
                                  UserManager<AppUser> userManager) =>
{
    var form = await http.Request.ReadFormAsync();
    var email = form["Email"].ToString();
    var password = form["Password"].ToString();

    var user = await userManager.FindByEmailAsync(email);
    if (user is null)
        return Results.Redirect("/login?e=1");

    var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
    if (!result.Succeeded)
        return Results.Redirect("/login?e=1");

    var returnUrl = http.Request.Query["returnUrl"].ToString();
    if (!string.IsNullOrWhiteSpace(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
        return Results.Redirect(returnUrl);

    return Results.Redirect("/");
}).AllowAnonymous();

app.MapPost("/auth/logout", async (SignInManager<AppUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
}).AllowAnonymous();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.Services.GetRequiredService<IAppSyncContext>().InitializeAsync();
app.Run();
