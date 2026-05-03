using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Infrastructure.Auth;

namespace Web.Bootstrap
{
    public static class OmniPortAuthEndpoints
    {
        public static WebApplication MapOmniPortAuthEndpoints(this WebApplication app)
        {
            app.MapPost("/auth/login", Login).AllowAnonymous();
            app.MapPost("/auth/logout", Logout).AllowAnonymous();
            return app;
        }

        private static async Task<IResult> Login(
            HttpContext http,
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("OmniPort.Auth");
            IFormCollection form = await http.Request.ReadFormAsync();
            string email = form["Email"].ToString();
            string password = form["Password"].ToString();

            AppUser? user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                logger.LogWarning("Login failed for unknown email {Email}", email);
                return Results.Redirect("/login?e=1");
            }

            SignInResult result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                logger.LogWarning("Login failed for user {UserId}", user.Id);
                return Results.Redirect("/login?e=1");
            }

            string returnUrl = http.Request.Query["returnUrl"].ToString();
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
            {
                logger.LogInformation("User {UserId} logged in and redirected to {ReturnUrl}", user.Id, returnUrl);
                return Results.Redirect(returnUrl);
            }

            logger.LogInformation("User {UserId} logged in", user.Id);
            return Results.Redirect("/");
        }

        private static async Task<IResult> Logout(
            SignInManager<AppUser> signInManager,
            ILoggerFactory loggerFactory)
        {
            await signInManager.SignOutAsync();
            loggerFactory.CreateLogger("OmniPort.Auth").LogInformation("User logged out");
            return Results.Redirect("/login");
        }
    }
}
