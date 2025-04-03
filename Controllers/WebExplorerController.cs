using Microsoft.AspNetCore.Mvc;
using RootBackend.Explorer.Services;

namespace RootBackend.Controllers
{
    [ApiController]
    [Route("api/web")]
    public class WebExplorerController : ControllerBase
    {
        private readonly RootNavigator _navigator;

        public WebExplorerController()
        {
            _navigator = new RootNavigator();
        }

        [HttpPost("navigate")]
        public async Task<IActionResult> ExploreUrl([FromBody] string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
                return BadRequest("URL invalide ou vide.");

            var result = await _navigator.ExplorePageAsync(url);

            return Ok(new { url, result });
        }
    }
}
