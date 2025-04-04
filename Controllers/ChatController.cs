using Microsoft.AspNetCore.Mvc;
using RootBackend.Explorer.Skills;
using RootBackend.Services;

namespace RootBackend.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly SkillDispatcher _dispatcher;

        public ChatController(SkillDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            var userId = User?.Identity?.IsAuthenticated == true
                ? User.Identity.Name ?? "connected"
                : request.UserId ?? "anonymous";

            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Message vide.");

            var reply = await _dispatcher.DispatchAsync(request.Message, userId);
            return Ok(new { reply });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
        public string? UserId { get; set; } // utile si pas loggué
    }

}