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
            // Créer un état unique pour cette requête
            var state = Guid.NewGuid().ToString();

            // Stocker cet état dans le cookie temporairement
            Response.Cookies.Append("GoogleOAuthState", state, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });

            // Utiliser l'URL HTTPS
            var callbackUrl = "https://api.rootai.fr/api/auth/google-callback";
            Console.WriteLine($"Redirection vers Google avec callback: {callbackUrl}");

            var properties = new AuthenticationProperties
            {
                RedirectUri = callbackUrl,
                // Utiliser l'état généré
                Items = { { ".xsrf", state } }
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            Console.WriteLine("Callback reçu de Google");

            // Vérifier si le state est présent dans la requête
            if (Request.Query.ContainsKey("state"))
            {
                Console.WriteLine($"State reçu: {Request.Query["state"]}");
            }
            else
            {
                Console.WriteLine("Aucun state reçu dans la requête");
            }
            foreach (var header in Request.Headers)
            {
                Console.WriteLine($"Header: {header.Key}={header.Value}");
            }

            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Console.WriteLine($"Authentification réussie: {result.Succeeded}");

            if (!result.Succeeded)
            {
                if (result.Failure?.Data != null)
                {
                    foreach (var failure in result.Failure.Data)
                    {
                        Console.WriteLine($"Erreur auth: {failure}");
                    }
                }

                return Unauthorized();
            }



            var claims = result.Principal.Identities
                .FirstOrDefault()?.Claims.Select(claim => new { claim.Type, claim.Value });

            return Ok(new
            {
                message = "Connexion réussie avec Google !",
                claims
            });
        }
    }
}
