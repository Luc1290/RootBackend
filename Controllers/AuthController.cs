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
            Console.WriteLine("[google-login-url] Début appel API");
            Console.WriteLine($"[google-login-url] User-Agent: {Request.Headers["User-Agent"]}");
            Console.WriteLine($"[google-login-url] Host: {Request.Host}");
            Console.WriteLine($"[google-login-url] Schéma: {Request.Scheme}");
            Console.WriteLine($"[google-login-url] Headers:");
            foreach (var header in Request.Headers)
                Console.WriteLine($"  {header.Key}: {header.Value}");

            var url = Url.Action("GoogleLogin", "Auth");
            var scheme = Request.Scheme;
            var host = Request.Host;
            var fullUrl = $"{scheme}://{host}/api/auth/google-login";

            Console.WriteLine($"[google-login-url] URL générée : {fullUrl}");

            return Ok(new { url = fullUrl });
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            Console.WriteLine("[google-login] Début Challenge() Google");
            var properties = new AuthenticationProperties
            {
                RedirectUri = "https://rootbackend.fly.dev/api/auth/google-callback"
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