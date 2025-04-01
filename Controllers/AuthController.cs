using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RootBackend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpGet("google-login-url")]
        public IActionResult GetGoogleLoginUrl()
        {
            var state = Guid.NewGuid().ToString();

            var props = new AuthenticationProperties
            {
                RedirectUri = "https://api.rootai.fr/api/auth/google-callback",
                Items = { { "XsrfId", state } }
            };

            Response.Cookies.Append("GoogleOAuthState", state, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                MaxAge = TimeSpan.FromMinutes(5)
            });

            var url = Url.Action("GoogleLogin", "Auth");
            var scheme = Request.Scheme;
            var host = Request.Host;
            var fullUrl = $"{scheme}://{host}/api/auth/google-login?state={state}";

            return Ok(new { url = fullUrl });
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin([FromQuery] string state)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "https://api.rootai.fr/api/auth/google-callback"
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            Console.WriteLine("Callback reçu de Google");
            Console.WriteLine($"URL complète: {Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}");

            try
            {
                var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                Console.WriteLine($"Authentification réussie: {result.Succeeded}");

                if (!result.Succeeded)
                {
                    Console.WriteLine($"Raison de l'échec: {result.Failure?.Message}");
                    return Redirect("https://rootai.fr/login?error=" + Uri.EscapeDataString("auth_failed: " + (result.Failure?.Message ?? "Unknown error")));
                }

                var claims = result.Principal.Identities
                    .FirstOrDefault()?.Claims.Select(claim => new { claim.Type, claim.Value }).ToList();

                Console.WriteLine($"Nombre de claims: {claims?.Count ?? 0}");

                return Redirect("https://rootai.fr/auth/callback");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans GoogleCallback: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Redirect("https://rootai.fr/login?error=" + Uri.EscapeDataString(ex.Message));
            }
        }

        [HttpGet("me")]
        public IActionResult Me()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new { authenticated = false, message = "Utilisateur non connecté" });
            }

            var name = User.Identity.Name;

            return Ok(new
            {
                authenticated = true,
                user = new
                {
                    name = name
                }
            });
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "L'API fonctionne correctement!", timestamp = DateTime.Now });
        }
    }
}