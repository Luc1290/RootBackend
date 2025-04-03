using Microsoft.Extensions.Logging;
using RootBackend.Models;
using RootBackend.Services;
using RootBackend.Utils;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using static RootBackend.Explorer.Skills.IntentionSkill;

namespace RootBackend.Explorer.Skills
{
    public class NavigatorSkill : IRootSkill
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NavigatorSkill> _logger;
        private readonly GroqService _groqService;
        private readonly MessageService _messageService;

        public NavigatorSkill(
            IHttpClientFactory httpClientFactory,
            ILogger<NavigatorSkill> logger,
            GroqService groqService,
            MessageService messageService)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _groqService = groqService;
            _messageService = messageService;
        }

        public bool CanHandle(string message)
        {
            // Implement the method to satisfy the interface
            var intention = IntentionSkill.IntentionParser.Parse(message);
            return CanHandle(intention);
        }

        public async Task<string?> HandleAsync(string message)
        {
            // Implement the method to satisfy the interface
            var intention = IntentionSkill.IntentionParser.Parse(message);
            return await HandleAsync(message, intention, "anonymous");
        }

        public bool CanHandle(ParsedIntention intention)
        {
            return intention.Intentions.Contains("websearch");
        }

        public async Task<string> HandleAsync(string userMessage, ParsedIntention context, string userId)
        {
            try
            {
                _logger.LogInformation($"[SCRAPER] 🔍 Requête reçue pour : \"{userMessage}\"");

                var client = _httpClientFactory.CreateClient();
                var payload = new
                {
                    query = userMessage
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://root-web-scraper.fly.dev/scrape", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[SCRAPER] ❌ Erreur HTTP : " + response.StatusCode);
                    return "Je n’ai pas pu obtenir de résultat pour cette recherche.";
                }

                var result = await response.Content.ReadAsStringAsync();

                // Prompt universel
                var prompt = $"""
            Tu es un assistant web intelligent. Voici le contenu extrait d’une page internet :

            ===================
            {result}
            ===================

            Ta tâche :
            - Résume les informations les plus utiles.
            - Réponds à la demande de l’utilisateur si possible.
            - Ignore les menus, cookies, mentions légales, ou tout contenu hors sujet.

            Message initial de l'utilisateur : 
            "{userMessage}"

            Ta réponse :
            """;

                var aiResponse = await _groqService.GetCompletionAsync(prompt);

                await _messageService.SaveUserMessageAsync(userMessage, "websearch", userId);
                await _messageService.SaveBotMessageAsync(aiResponse, "websearch", userId);

                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SCRAPER] ❌ Erreur lors du traitement de la requête");
                return "Une erreur est survenue pendant la navigation. Réessaie dans un instant.";
            }
        }
    }

}
