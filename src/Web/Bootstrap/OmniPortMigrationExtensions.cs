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

            var db = scope.ServiceProvider.GetRequiredService<OmniPortDataContext>();
            await db.Database.MigrateAsync();

            var identityDb = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
            await identityDb.Database.MigrateAsync();
        }
    }
}
