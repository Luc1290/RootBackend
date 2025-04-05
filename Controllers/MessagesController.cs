using Microsoft.AspNetCore.Mvc;
using RootBackend.Core;
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
        private readonly WebScraperService _webScraper;

        public MessagesController(
            MessageService messageService,
            NlpService nlpService,
            GroqService groq,
            WebScraperService webScraper)
        {
            _messageService = messageService;
            _nlp = nlpService;
            _groq = groq;
            _webScraper = webScraper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageLog>>> GetMessages(string? userId = null)
        {
            return await _messageService.GetRecentMessagesAsync(50, userId);
        }

        [HttpPost]
        public async Task<ActionResult<MessageLog>> PostMessage(MessageLog message)
        {
            string userId = "anonymous";
            if (User.Identity?.IsAuthenticated == true)
            {
                userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "anonymous";
            }

            message.Id = Guid.NewGuid();
            message.Timestamp = DateTime.UtcNow;
            message.UserId = userId;

            await _messageService.SaveUserMessageAsync(message.Content, "messages", userId);

            var nlpResult = await _nlp.AnalyzeAsync(message.Content);
            if (nlpResult == null || string.IsNullOrWhiteSpace(nlpResult.Intent))
            {
                return StatusCode(500, "Erreur NLP");
            }

            var aiReply = await IntentRouter.HandleAsync(nlpResult, message.Content, _groq, _webScraper);

            var response = await _messageService.SaveBotMessageAsync(aiReply, "ai", userId);
            return Ok(response);
        }

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
