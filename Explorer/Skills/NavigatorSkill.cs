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
            return intention.Intentions.Contains("websearch")
                || intention.Intentions.Contains("info")
                || intention.Intentions.Contains("actualité")
                || intention.Intentions.Contains("météo")
                || !intention.Intentions.Any(); // si rien de spécifique détecté, on prend !
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

                var pageContent = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(pageContent) || pageContent.Length < 100)
                {
                    _logger.LogWarning("[SCRAPER] 📭 Contenu HTML vide ou insuffisant.");
                    return "Je n’ai pas trouvé cette information sur la page.";
                }

                // 🧠 Prompt unique et polyvalent
                var prompt = $"""
Tu es un agent de lecture web très rigoureux.

Tu reçois le contenu HTML d’une page web. Ta mission est d’analyser ce contenu **et uniquement ce contenu** pour en tirer des informations précises.

Voici la demande de l’utilisateur :
{userMessage}

Voici le texte extrait de la page HTML :
{pageContent}

Ta réponse doit :
- Être **factuelle**, basée uniquement sur ce que tu trouves dans le texte.
- Ne jamais conseiller l'utilisateur d'aller sur un site, utiliser une API ou une application.
- Ne jamais proposer de code ou d’alternative de recherche.
- Ne rien inventer si l'information n’est pas clairement indiquée.
- Si la donnée n’est pas trouvable, réponds simplement : **“Je n’ai pas trouvé cette information sur la page.”**

Tu peux utiliser des puces, titres, tableaux, ou une réponse directe si besoin. Mais reste toujours fidèle au contenu fourni.
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
