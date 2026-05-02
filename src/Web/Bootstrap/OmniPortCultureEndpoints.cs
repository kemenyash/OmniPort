using Web.Localization;

namespace Web.Bootstrap
{
    public static class OmniPortCultureEndpoints
    {
        public static WebApplication MapOmniPortCultureEndpoints(this WebApplication app)
        {
            app.MapGet("/culture/{culture}", SetCulture).AllowAnonymous();
            return app;
        }

        private static IResult SetCulture(HttpContext httpContext, string culture, string? returnUrl)
        {
            var normalizedCulture = AppLocalizer.NormalizeCulture(culture);

            httpContext.Response.Cookies.Append(
                AppLocalizer.CookieName,
                normalizedCulture,
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = false,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                });

            if (string.IsNullOrWhiteSpace(returnUrl) ||
                !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
            {
                returnUrl = "/";
            }

            return Results.Redirect(returnUrl);
        }
    }
}
