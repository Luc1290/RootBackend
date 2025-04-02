using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace RootBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var identity = User.Identity;
        if (identity == null || !identity.IsAuthenticated)
        {
            return Unauthorized(new { authenticated = false, message = "Utilisateur non connecté" });
        }

        // Extraction de l'email depuis les claims
        var email = User.Claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.Email ||
            c.Type == "email" ||
            c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
        )?.Value;

        // Format cohérent avec ce qu'attend le frontend
        return Ok(new
        {
            authenticated = true,
            user = new
            {
                name = identity.Name,
                email = email
            }
        });
    }
}