using Microsoft.AspNetCore.Mvc;
using RootBackend.Models;
using RootBackend.Services;
using System.Security.Claims;

namespace RootBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly MessageService _messageService;
        private readonly NlpService _nlp;
        private readonly GroqService _groq;


        public MessagesController(MessageService messageService, NlpService nlpService, GroqService groq)

        {
            _messageService = messageService;
            _nlp = nlpService;
            _groq = groq;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageLog>>> GetMessages(string? userId = null)
        {
            // Si userId est spécifié, récupérer les messages de cet utilisateur
            // Sinon, récupérer tous les messages récents
            return await _messageService.GetRecentMessagesAsync(50, userId);
        }

        [HttpPost]
        public async Task<ActionResult<MessageLog>> PostMessage(MessageLog message)
        {
            // Récupérer l'identifiant de l'utilisateur connecté, ou "anonymous" si non connecté
            string userId = "anonymous";
            if (User.Identity?.IsAuthenticated == true)
            {
                userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "anonymous";
            }

            // Mise à jour des propriétés du message
            message.Id = Guid.NewGuid();
            message.Timestamp = DateTime.UtcNow;
            message.UserId = userId; // Définir l'ID utilisateur

            // Sauvegarde du message utilisateur via le service
            var savedMessage = await _messageService.SaveUserMessageAsync(message.Content, "messages", userId);

            // Appel à l'API NLP pour analyser le message  //
            var nlpResult = await _nlp.AnalyzeAsync(message.Content);
            if (nlpResult == null || string.IsNullOrWhiteSpace(nlpResult.Prompt))
            {
                return StatusCode(500, "Erreur NLP");
            }

            // Appel à l'API Groq pour générer une réponse //
            var aiReply = await _groq.GetCompletionAsync(nlpResult.Prompt); // ✅ propre, unique



            // Sauvegarde de la réponse de l'IA via le service
            var response = await _messageService.SaveBotMessageAsync(aiReply, "ai", userId);
            return Ok(response);
        }

        // Nouvelle méthode pour récupérer les conversations d'un utilisateur spécifique
        [HttpGet("conversation")]
        public async Task<ActionResult<IEnumerable<MessageLog>>> GetUserConversation(string? userId = null)
        {
            if (string.IsNullOrEmpty(userId) && User.Identity?.IsAuthenticated == true)
            {
                userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            }

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("L'identifiant utilisateur est requis");
            }

            return await _messageService.GetUserConversationAsync(userId, 100);
        }
    }
}