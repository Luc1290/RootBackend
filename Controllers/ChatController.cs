using Microsoft.AspNetCore.Mvc;
using RootBackend.Explorer.Skills;
using RootBackend.Services;
using RootBackend.Explorer.Models;
using RootBackend.Data;
using RootBackend.Models;

namespace RootBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IEnumerable<IRootSkill> _skills;
        private readonly GroqService _saba;
        private readonly MemoryContext _context;

        public ChatController(IEnumerable<IRootSkill> skills, GroqService saba, MemoryContext context)
        {
            _skills = skills;
            _saba = saba;
            _context = context;
        }

        public class ChatRequest
        {
            public string Message { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatRequest request)
        {
            var message = request.Message;

            // Sauvegarde du message de l'utilisateur
            var userMessageLog = new MessageLog
            {
                Id = Guid.NewGuid(),
                Content = message,
                Sender = "user",
                Timestamp = DateTime.UtcNow,
                Type = "text",
                Source = "chat"
            };

            _context.Messages.Add(userMessageLog);
            await _context.SaveChangesAsync();

            string response;
            string source;

            // 🔍 1. Interception par un skill
            foreach (var skill in _skills)
            {
                Console.WriteLine("🔍 Skill testé : " + skill.GetType().Name);
                if (skill.CanHandle(message))
                {
                    response = await skill.HandleAsync(message);
                    if (!string.IsNullOrWhiteSpace(response))
                    {
                        Console.WriteLine("✅ Réponse d'un skill : " + response);
                        source = "skill";

                        // Sauvegarde de la réponse du skill
                        var skillMessageLog = new MessageLog
                        {
                            Id = Guid.NewGuid(),
                            Content = response,
                            Sender = "bot",
                            Timestamp = DateTime.UtcNow,
                            Type = "text",
                            Source = source
                        };

                        _context.Messages.Add(skillMessageLog);
                        await _context.SaveChangesAsync();

                        return Ok(new { reply = response });
                    }
                }
            }

            // 🤖 2. Sinon, envoi à Saba (Groq)
            try
            {
                response = await _saba.GetCompletionAsync(message);
                source = "ai";
                Console.WriteLine("🤖 Réponse de Saba : " + response);

                // Sauvegarde de la réponse de l'IA
                var aiMessageLog = new MessageLog
                {
                    Id = Guid.NewGuid(),
                    Content = response,
                    Sender = "bot",
                    Timestamp = DateTime.UtcNow,
                    Type = "text",
                    Source = source
                };

                _context.Messages.Add(aiMessageLog);
                await _context.SaveChangesAsync();

                return Ok(new { reply = response });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Erreur appel Saba : " + ex.Message);
                return StatusCode(500, "Erreur serveur lors de la génération de la réponse.");
            }
        }
    }
}