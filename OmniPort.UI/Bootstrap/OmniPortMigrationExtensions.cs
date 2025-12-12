using Microsoft.EntityFrameworkCore;
using OmniPort.Data;
using OmniPort.Data.Auth;

namespace OmniPort.UI.Bootstrap
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
