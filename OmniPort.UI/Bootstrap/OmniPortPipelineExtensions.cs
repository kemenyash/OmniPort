namespace OmniPort.UI.Bootstrap
{
    public static class OmniPortPipelineExtensions
    {
        public static WebApplication UseOmniPortPipeline(this WebApplication app)
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();

            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }
    }
}
