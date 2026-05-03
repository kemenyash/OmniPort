using BusinessLogic.Interfaces;
using Infrastructure.Auth;
using Infrastructure.Parsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class InfrastructureConfiguration
    {
        public static IServiceCollection AddOmniPortInfrastructure(
            this IServiceCollection services,
            string connectionString)
        {
            services.AddDbContext<OmniPortDataContext>(options =>
                options.UseSqlite(connectionString));

            services.AddDbContext<AppIdentityDbContext>(options =>
                options.UseSqlite(connectionString));

            services.AddScoped<IImportParserFactory, ImportParserFactory>();

            return services;
        }
    }
}
