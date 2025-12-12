using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using OmniPort.Data.Auth;

namespace OmniPort.UI.Bootstrap
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
            UserManager<AppUser> userManager)
        {
            IFormCollection form = await http.Request.ReadFormAsync();
            string email = form["Email"].ToString();
            string password = form["Password"].ToString();

            AppUser? user = await userManager.FindByEmailAsync(email);
            if (user is null) return Results.Redirect("/login?e=1");

            SignInResult result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);

            if (!result.Succeeded) return Results.Redirect("/login?e=1");

            string returnUrl = http.Request.Query["returnUrl"].ToString();
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
            {
                return Results.Redirect(returnUrl);
            }

            return Results.Redirect("/");
        }

        private static async Task<IResult> Logout(SignInManager<AppUser> signInManager)
        {
            await signInManager.SignOutAsync();
            return Results.Redirect("/login");
        }
    }
}
