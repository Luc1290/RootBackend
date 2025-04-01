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
        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
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

            // Ajouter plus de logging pour diagnostic
            Console.WriteLine("Headers présents:");
            foreach (var header in Request.Headers)
            {
                Console.WriteLine($"Header: {header.Key} = {header.Value}");
            }

            Console.WriteLine("Cookies présents:");
            foreach (var cookie in Request.Cookies)
            {
                Console.WriteLine($"Cookie: {cookie.Key} = {cookie.Value}");
            }

            try
            {
                // Récupérer l'état de la requête pour diagnostic
                var requestState = Request.Query["state"].ToString();
                Console.WriteLine($"État reçu dans la requête: {requestState}");

                // Récupérer le state stocké dans le cookie de secours
                var savedState = Request.Cookies["GoogleStateToken"];
                Console.WriteLine($"État stocké dans le cookie de secours: {savedState}");

                // Procéder à l'authentification
                var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                Console.WriteLine($"Authentification réussie: {result.Succeeded}");

                if (!result.Succeeded)
                {
                    Console.WriteLine($"Raison de l'échec: {result.Failure?.Message}");
                    return Redirect("https://api.rootai.fr/login?error=" + Uri.EscapeDataString("auth_failed: " + (result.Failure?.Message ?? "Unknown error")));
                }

                var claims = result.Principal.Identities
                    .FirstOrDefault()?.Claims.Select(claim => new { claim.Type, claim.Value }).ToList();

                Console.WriteLine($"Nombre de claims: {claims?.Count ?? 0}");

                // Rediriger vers la page d'accueil du frontend après une connexion réussie
                return Redirect("https://rootai.fr/auth/callback");

            }
            catch (Exception ex)
            {
                // Capture et log les exceptions
                Console.WriteLine($"Exception dans GoogleCallback: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Redirect("https://api.rootai.fr/login?error=" + Uri.EscapeDataString(ex.Message));
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