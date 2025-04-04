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
            Console.WriteLine($"📥 Nouvelle requête reçue : {request.Message}, UserId = {request.UserId}");

            var userId = User?.Identity?.IsAuthenticated == true
                ? User.Identity.Name ?? "connected"
                : request.UserId ?? "anonymous";

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                Console.WriteLine("⚠️ Message vide.");
                return BadRequest("Message vide.");
            }

            try
            {
                var reply = await _dispatcher.DispatchAsync(request.Message, userId);
                Console.WriteLine($"✅ Réponse générée : {reply}");
                return Ok(new { reply });
            }
            catch (Exception ex)
            {
                Console.WriteLine("💥 Erreur dans ChatController:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, "Une erreur est survenue.");
            }
        }

    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
        public string? UserId { get; set; } // utile si pas loggué
    }

}