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

        private static IResult Latest(
            int watchedUrlId,
            IAppSyncContext syncContext,
            ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("OmniPort.WatchEndpoint");
            var watchedUrl = syncContext.WatchedUrls.FirstOrDefault(x => x.Id == watchedUrlId);
            if (watchedUrl is null)
            {
                logger.LogWarning("Latest watched URL request missed unknown id {WatchedUrlId}", watchedUrlId);
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
                logger.LogInformation("Latest conversion is not available for watched URL {WatchedUrlId}", watchedUrlId);
                return Results.NotFound();
            }

            logger.LogInformation(
                "Redirecting watched URL {WatchedUrlId} to latest output {OutputLink}",
                watchedUrlId,
                latestConversion.OutputLink);
            return Results.Redirect(latestConversion.OutputLink);
        }
    }
}
