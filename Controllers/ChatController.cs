using Microsoft.AspNetCore.Mvc;
using RootBackend.Explorer.Skills;
using RootBackend.Services;
using RootBackend.Explorer.Models;

namespace RootBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IEnumerable<IRootSkill> _skills;
        private readonly GroqService _saba;

        public ChatController(IEnumerable<IRootSkill> skills, GroqService saba)
        {
            _skills = skills;
            _saba = saba;
        }

        public class ChatRequest
        {
            public string Message { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatRequest request)
        {
            var message = request.Message;

            // 🔍 1. Interception par un skill
            foreach (var skill in _skills)
            {
                if (skill.CanHandle(message))
                {
                    var response = await skill.HandleAsync(message);
                    if (!string.IsNullOrWhiteSpace(response))
                    {
                        Console.WriteLine("✅ Réponse d’un skill : " + response);
                        return Ok(new { reply = response });
                    }
                }
            }

            // 🤖 2. Sinon, envoi à Saba (Groq)
            try
            {
                var fullResponse = await _saba.GetCompletionAsync(message);
                Console.WriteLine("🤖 Réponse de Saba : " + fullResponse);
                return Ok(new { response = fullResponse });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Erreur appel Saba : " + ex.Message);
                return StatusCode(500, "Erreur serveur lors de la génération de la réponse.");
            }
        }
    }
}
