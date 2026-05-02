using BusinessLogic.Interfaces;

namespace Web.Bootstrap
{
    public static class OmniPortWatchEndpoints
    {
        public static WebApplication MapOmniPortWatchEndpoints(this WebApplication app)
        {
            app.MapGet("/watch/{watchedUrlId:int}/latest", Latest);
            return app;
        }

        private static IResult Latest(int watchedUrlId, IAppSyncContext syncContext)
        {
            var watchedUrl = syncContext.WatchedUrls.FirstOrDefault(x => x.Id == watchedUrlId);
            if (watchedUrl is null)
            {
                return Results.NotFound();
            }

            var latestConversion = syncContext.UrlConversions
                .Where(x =>
                    x.MappingTemplateId == watchedUrl.MappingTemplateId &&
                    string.Equals(x.InputUrl, watchedUrl.Url, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.ConvertedAt)
                .FirstOrDefault();

            if (latestConversion is null || string.IsNullOrWhiteSpace(latestConversion.OutputLink))
            {
                return Results.NotFound();
            }

            return Results.Redirect(latestConversion.OutputLink);
        }
    }
}
