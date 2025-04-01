using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RootBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult Get()
    {
        var identity = User.Identity;
        if (identity == null || !identity.IsAuthenticated)
        {
            return Unauthorized();
        }

        var claims = User.Claims.ToDictionary(c => c.Type, c => c.Value);
        return Ok(new
        {
            IsAuthenticated = identity.IsAuthenticated,
            Name = identity.Name,
            Claims = claims
        });
    }
}
