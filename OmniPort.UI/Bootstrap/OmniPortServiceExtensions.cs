using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniPort.Core.Interfaces;
using OmniPort.Data;
using OmniPort.Data.Auth;
using OmniPort.UI.Presentation;
using OmniPort.UI.Presentation.Mapping;
using OmniPort.UI.Presentation.Models;
using OmniPort.UI.Presentation.Services;
using OmniPort.UI.Presentation.ViewModels.Components;
using OmniPort.UI.Presentation.ViewModels.Pages;

namespace OmniPort.UI.Bootstrap
{
    public static class OmniPortServiceExtensions
    {
        public static IServiceCollection AddOmniPort(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            const string connectionString = "Data Source=omniport.db";

            services.AddDbContext<OmniPortDataContext>(o =>
                o.UseSqlite(connectionString));

            services.AddDbContext<AppIdentityDbContext>(o =>
                o.UseSqlite(connectionString));

            services.AddAutoMapper(cfg =>
                cfg.AddProfile<OmniPortMappingProfile>());

            services.Configure<UploadLimits>(
                configuration.GetSection("UploadLimits"));

            services.AddRazorComponents()
                    .AddInteractiveServerComponents();

            services.AddHttpClient();

            services.AddScoped<ITemplateManager, TemplateManager>();
            services.AddScoped<ITransformationManager, TransformationManager>();
            services.AddScoped<ITransformationExecutionService, TransformationExecutor>();

            services.AddScoped<LoginViewModel>();
            services.AddScoped<IndexViewModel>();
            services.AddScoped<ErrorViewModel>();
            services.AddScoped<FieldRowEditorViewModel>();
            services.AddScoped<TemplateEditorViewModel>();
            services.AddScoped<JoinTemplatesViewModel>();
            services.AddScoped<TransformationViewModel>();

            services.AddSingleton<IAppSyncContext, AppSyncContext>();
            services.AddSingleton<ISourceFingerprintStore, InMemorySourceFingerprintStore>();
            services.AddHostedService<WatchedHashSyncService>();

            services.AddAuthentication(IdentityConstants.ApplicationScheme)
                    .AddIdentityCookies();

            services.Configure<CookieAuthenticationOptions>(
                IdentityConstants.ApplicationScheme, options =>
                {
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/login";
                    options.ReturnUrlParameter = "returnUrl";
                });

            services.AddAuthorization();

            services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<AppIdentityDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

            services.AddEndpointsApiExplorer();

            return services;
        }
    }
}
