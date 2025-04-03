using Microsoft.Extensions.Logging;
using RootBackend.Models;
using RootBackend.Services;
using System.Text.Json;
using System.Text;
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
            var intention = IntentionParser.Parse(message);
            return CanHandle(intention);
        }

        public async Task<string?> HandleAsync(string message)
        {
            var intention = IntentionParser.Parse(message);
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
                var payload = new { query = userMessage };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://root-web-scraper.fly.dev/scrape", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[SCRAPER] ❌ Erreur HTTP : " + response.StatusCode);
                    return "Je n’ai pas pu obtenir de résultat pour cette recherche.";
                }

                var result = await response.Content.ReadAsStringAsync();

                // 🧠 Prompt unique et polyvalent
                var prompt = $"""
                Tu es un assistant intelligent avec un accès à internet.

                Voici le contenu d’une page web que j’ai visitée pour répondre à la question suivante :
                "{userMessage}"

                ====================
                {result}
                ====================

                Donne une réponse synthétique, claire, et utile à l’utilisateur. Ignore les menus, publicités, cookies, mentions légales ou sections inutiles.
                Si aucune info utile n’est trouvée, dis-le simplement.
                """;

                var aiResponse = await _groqService.GetCompletionAsync(prompt);
                _logger.LogInformation("[SCRAPER] ✅ Réponse IA : " + aiResponse.Substring(0, Math.Min(200, aiResponse.Length)) + "...");

                await _messageService.SaveUserMessageAsync(userMessage, "websearch", userId);
                await _messageService.SaveBotMessageAsync(aiResponse, "websearch", userId);

                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SCRAPER] ❌ Erreur pendant la navigation");
                return "Une erreur est survenue pendant la navigation. Réessaie dans un instant.";
            }
        }
    }
}
