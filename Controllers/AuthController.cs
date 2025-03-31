using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

namespace RootBackend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            // Pas de gestion d'état manuelle
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

            // Pour debug, imprimez la requête complète
            Console.WriteLine($"URL complète: {Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}");

            // Vérifier si le state est présent dans la requête
            if (Request.Query.ContainsKey("state"))
            {
                Console.WriteLine($"State reçu: {Request.Query["state"]}");

                // Vérifier si le state dans le cookie correspond
                if (Request.Cookies.TryGetValue("GoogleOAuthState", out var storedState))
                {
                    Console.WriteLine($"State stocké dans le cookie: {storedState}");

                    if (storedState != Request.Query["state"])
                    {
                        Console.WriteLine("Les états ne correspondent pas!");
                    }
                }
                else
                {
                    Console.WriteLine("Aucun state trouvé dans les cookies");
                }
            }
            else
            {
                Console.WriteLine("Aucun state reçu dans la requête");
            }

            // Afficher les headers pour le débogage
            foreach (var header in Request.Headers)
            {
                Console.WriteLine($"Header: {header.Key}={header.Value}");
            }

            // Afficher tous les cookies reçus
            Console.WriteLine("Cookies reçus:");
            foreach (var cookie in Request.Cookies)
            {
                Console.WriteLine($"Cookie: {cookie.Key}={cookie.Value}");
            }

            try
            {
                var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                Console.WriteLine($"Authentification réussie: {result.Succeeded}");

                if (!result.Succeeded)
                {
                    Console.WriteLine($"Raison de l'échec: {result.Failure?.Message}");

                    if (result.Failure?.Data != null)
                    {
                        foreach (var failure in result.Failure.Data)
                        {
                            Console.WriteLine($"Erreur auth: {failure}");
                        }
                    }

                    return Redirect("/login?error=auth_failed");
                }

                var claims = result.Principal.Identities
                    .FirstOrDefault()?.Claims.Select(claim => new { claim.Type, claim.Value }).ToList();

                Console.WriteLine($"Nombre de claims: {claims?.Count ?? 0}");

                // Rediriger vers la page d'accueil après une connexion réussie
                return Redirect("/");
            }
            catch (Exception ex)
            {
                // Capture et log les exceptions
                Console.WriteLine($"Exception dans GoogleCallback: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Redirect("/login?error=" + Uri.EscapeDataString(ex.Message));
            }
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "L'API fonctionne correctement!", timestamp = DateTime.Now });
        }
    }
}