using Explorer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RootBackend.Data;
using RootBackend.Explorer.Skills;
using RootBackend.Models;
using RootBackend.Services;


namespace RootBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly MemoryContext _context;
        private readonly GroqService _groq;
        private readonly WeatherSkill _weatherSkill;

        public MessagesController(MemoryContext context, GroqService groq, WeatherSkill weatherSkill)
        {
            _context = context;
            _groq = groq;
            _weatherSkill = weatherSkill;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageLog>>> GetMessages()
        {
            return await _context.Messages
                .OrderByDescending(m => m.Timestamp)
                .Take(50)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<MessageLog>> PostMessage(MessageLog message)
        {
            message.Id = Guid.NewGuid();
            message.Timestamp = DateTime.UtcNow;
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // === [1] Vérifier si une skill peut répondre ===
            var skillResponse = await _weatherSkill.HandleAsync(message.Content);

            if (!string.IsNullOrWhiteSpace(skillResponse))
            {
                var reply = new MessageLog
                {
                    Id = Guid.NewGuid(),
                    Content = skillResponse,
                    Sender = "bot",
                    Timestamp = DateTime.UtcNow,
                    Type = "text",
                    Source = "skill"
                };

                _context.Messages.Add(reply);
                await _context.SaveChangesAsync();

                return Ok(reply);
            }

            // === [2] Sinon on continue avec l'IA ===
            var aiReply = await _groq.GetCompletionAsync(message.Content);

            var response = new MessageLog
            {
                Id = Guid.NewGuid(),
                Content = aiReply,
                Sender = "bot",
                Timestamp = DateTime.UtcNow,
                Type = "text",
                Source = "ai"
            };

            _context.Messages.Add(response);
            await _context.SaveChangesAsync();

            return Ok(response);
        }
    }
}
