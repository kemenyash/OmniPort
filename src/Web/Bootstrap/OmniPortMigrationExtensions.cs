using Microsoft.EntityFrameworkCore;
using Infrastructure;
using Infrastructure.Auth;

namespace Web.Bootstrap
{
    public static class OmniPortMigrationExtensions
    {
        public static async Task MigrateOmniPortDatabasesAsync(this WebApplication app)
        {
            using IServiceScope scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("OmniPort.Migrations");

            var db = scope.ServiceProvider.GetRequiredService<OmniPortDataContext>();
            logger.LogInformation("Applying migrations for {DbContext}", nameof(OmniPortDataContext));
            await db.Database.MigrateAsync();

            var identityDb = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
            logger.LogInformation("Applying migrations for {DbContext}", nameof(AppIdentityDbContext));
            await identityDb.Database.MigrateAsync();
            logger.LogInformation("Database migrations completed");
        }
    }
}
