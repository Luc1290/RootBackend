using Microsoft.AspNetCore.Mvc;
using RootBackend.Explorer.Skills;
using RootBackend.Models;
using RootBackend.Services;

namespace RootBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly MessageService _messageService;
        private readonly GroqService _groq;
        private readonly WeatherSkill _weatherSkill;

        public MessagesController(MessageService messageService, GroqService groq, WeatherSkill weatherSkill)
        {
            _messageService = messageService;
            _groq = groq;
            _weatherSkill = weatherSkill;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageLog>>> GetMessages()
        {
            return await _messageService.GetRecentMessagesAsync();
        }

        [HttpPost]
        public async Task<ActionResult<MessageLog>> PostMessage(MessageLog message)
        {
            // Mise à jour des propriétés du message
            message.Id = Guid.NewGuid();
            message.Timestamp = DateTime.UtcNow;

            // Sauvegarde du message utilisateur via le service
            var savedMessage = await _messageService.SaveUserMessageAsync(message.Content, "messages");

            // === [1] Vérifier si une skill peut répondre ===
            var skillResponse = await _weatherSkill.HandleAsync(message.Content);

            if (!string.IsNullOrWhiteSpace(skillResponse))
            {
                // Sauvegarde de la réponse du skill via le service
                var reply = await _messageService.SaveBotMessageAsync(skillResponse, "skill");
                return Ok(reply);
            }

            // === [2] Sinon on continue avec l'IA ===
            var aiReply = await _groq.GetCompletionAsync(message.Content);

            // Sauvegarde de la réponse de l'IA via le service
            var response = await _messageService.SaveBotMessageAsync(aiReply, "ai");
            return Ok(response);
        }
    }
}