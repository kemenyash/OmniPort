using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Infrastructure;
using Infrastructure.Auth;
using Presentation;

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
