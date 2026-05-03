using Web.Telemetry;

namespace Web.Bootstrap
{
    public static class OmniPortPipelineExtensions
    {
        public static WebApplication UseOmniPortPipeline(this WebApplication app)
        {
            var logger = app.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("OmniPort.Pipeline");

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
                logger.LogInformation("Production exception handling and HSTS enabled");
            }
            else
            {
                logger.LogInformation("Development pipeline enabled");
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseHealthChecks("/health");

            logger.LogInformation("OmniPort middleware pipeline configured");

            return app;
        }
    }
}
