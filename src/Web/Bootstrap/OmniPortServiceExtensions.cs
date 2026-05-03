using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Infrastructure;
using Infrastructure.Auth;
using Presentation;
using Web.Localization;
using Web.Telemetry;

namespace Web.Bootstrap
{
    public static class OmniPortServiceExtensions
    {
        public static IServiceCollection AddOmniPort(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            const string connectionString = "Data Source=omniport.db";

            services.AddOmniPortInfrastructure(connectionString);
            services.AddOmniPortPresentation(configuration);

            services.AddRazorComponents()
                    .AddInteractiveServerComponents();

            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddScoped<IAppLocalizer, AppLocalizer>();
            services.Configure<ApplicationLogStoreOptions>(
                configuration.GetSection("RuntimeLogging"));
            services.AddSingleton<ApplicationLogStore>();
            services.AddSingleton<IApplicationLogStore>(sp =>
                sp.GetRequiredService<ApplicationLogStore>());
            services.AddSingleton<ILoggerProvider, FileBackedLoggerProvider>();
            services.AddHealthChecks();
            services.AddOmniPortOpenTelemetry(serviceName: "omniport-web");

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
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppIdentityDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

            services.AddEndpointsApiExplorer();

            return services;
        }
    }
}
