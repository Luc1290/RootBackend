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
            // Utilisez exactement cette URL pour correspondre à ce que Google reçoit
            var callbackUrl = "https://api.rootai.fr/api/auth/google-callback";

            var properties = new AuthenticationProperties
            {
                RedirectUri = callbackUrl
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            Console.WriteLine("Callback reçu de Google");
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
