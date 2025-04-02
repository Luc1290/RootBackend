using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
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
            var properties = new AuthenticationProperties
            {
                // URL complète pour le callback
                RedirectUri = "https://api.rootai.fr/api/auth/google-callback",
                // Renforcer la sécurité de l'état OAuth
                Items = { { ".xsrf", Guid.NewGuid().ToString() } }
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

                if (result.Succeeded)
                {
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        result.Principal,
                        result.Properties);

                    // Rediriger vers le frontend - utilisez la route exacte configurée dans votre App.jsx
                    return Redirect("https://rootai.fr/auth/callback");
                }

                return Redirect("https://rootai.fr/login?error=authentication_failed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans GoogleCallback: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Redirect($"https://rootai.fr/login?error={Uri.EscapeDataString(ex.Message)}");
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
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            return Ok(new
            {
                authenticated = true,
                user = new
                {
                    name = name,
                    email = email
                }
            });
        }


        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "L'API fonctionne correctement!", timestamp = DateTime.Now });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Optionnel : supprimer les cookies manuellement côté serveur
            Response.Cookies.Delete("RootAI.Auth", new CookieOptions
            {
                Domain = ".rootai.fr",
                Secure = true,
                SameSite = SameSiteMode.None
            });

            // Réponse simple
            return Ok(new { message = "Déconnexion réussie" });
        }

    }
}