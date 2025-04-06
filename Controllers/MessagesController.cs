using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RootBackend.Core;
using RootBackend.Models;
using RootBackend.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

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
        private readonly ILogger<MessagesController> _logger;
        private readonly IntentRouter _intentRouter;


        public MessagesController(
            MessageService messageService,
            NlpService nlpService,
            GroqService groq,
            WebScraperService webScraper,
            ILogger<MessagesController> logger,
            IntentRouter intentRouter) 
        {
            _messageService = messageService;
            _nlp = nlpService;
            _groq = groq;
            _webScraper = webScraper;
            _logger = logger;
            _intentRouter = intentRouter; 
        }

        // GET api/messages
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageLog>>> GetMessages(string? userId = null)
        {
            try
            {
                // Déterminer l'utilisateur
                _logger.LogInformation("Récupération des messages pour utilisateur: {UserId}", userId ?? "tous");
                return await _messageService.GetRecentMessagesAsync(50, userId);
            }
            catch (Exception ex)
            {
                // En cas d'erreur, loguer et retourner une réponse d'erreur
                _logger.LogError(ex, "Erreur lors de la récupération des messages");
                return StatusCode(500, new { error = "Erreur serveur lors de la récupération des messages" });
            }
        }

        // POST api/messages
        [HttpPost]
        public async Task<ActionResult<MessageLog>> PostMessage(MessageLog message)
        {
            if (string.IsNullOrWhiteSpace(message?.Content))
            {
                return BadRequest(new { error = "Le contenu du message ne peut pas être vide" });
            }

            try
            {
                // Déterminer l'utilisateur
                string userId = "anonymous";
                if (User.Identity?.IsAuthenticated == true)
                {
                    userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "anonymous";
                }

                _logger.LogInformation("Message reçu de {UserId}: {Content}", userId, message.Content);

                // Initialiser correctement le message
                message.Id = Guid.NewGuid();
                message.Timestamp = DateTime.UtcNow;
                message.UserId = userId;

                // Enregistrer le message
                await _messageService.SaveUserMessageAsync(message.Content, "messages", userId);

                // Analyser l'intention
                var nlpResult = await _nlp.AnalyzeAsync(message.Content);
                if (nlpResult == null)
                {
                    _logger.LogWarning("Analyse NLP échouée, utilisation de l'intention par défaut");
                    nlpResult = new NlpResponse { Intent = "discussion" };
                }

                _logger.LogInformation("Intention détectée: {Intent}", nlpResult.Intent);

                // Obtenir la réponse selon l'intention
                var aiReply = await _intentRouter.HandleAsync(nlpResult, message.Content, _logger);


                // Enregistrer et retourner la réponse
                var response = await _messageService.SaveBotMessageAsync(aiReply, "ai", userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement du message");

                // Essayer d'envoyer une réponse de secours en cas d'erreur
                try
                {
                    string userId = User.Identity?.IsAuthenticated == true
                        ? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "anonymous"
                        : "anonymous";

                    string errorReply = "Désolé, j'ai rencontré un problème technique. Pourriez-vous reformuler votre question?";
                    var errorResponse = await _messageService.SaveBotMessageAsync(errorReply, "error", userId);

                    return Ok(errorResponse);
                }
                catch
                {
                    // Si même la réponse d'erreur échoue, retourner une erreur HTTP
                    return StatusCode(500, new { error = "Une erreur est survenue lors du traitement de votre message" });
                }
            }
        }

        // Endpoint pour récupérer la dernière réponse du bot
        [HttpGet("conversation")]
        public async Task<ActionResult<IEnumerable<MessageLog>>> GetUserConversation(string? userId = null)
        {
            try
            {
                // Déterminer l'utilisateur
                if (string.IsNullOrEmpty(userId) && User.Identity?.IsAuthenticated == true)
                {
                    userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { error = "L'identifiant utilisateur est requis" });
                }

                _logger.LogInformation("Récupération de la conversation pour {UserId}", userId);
                return await _messageService.GetUserConversationAsync(userId, 100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la conversation");
                return StatusCode(500, new { error = "Erreur serveur lors de la récupération de la conversation" });
            }
        }

        // Endpoint pour tester le système
        [HttpGet("health")]
        public ActionResult<object> HealthCheck()
        {
            return new
            {
                status = "ok",
                controller = "MessagesController",
                timestamp = DateTime.UtcNow
            };
        }
    }
}