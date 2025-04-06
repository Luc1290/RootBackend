using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RootBackend.Services;

namespace RootBackend.Core.IntentHandlers
{
    public class WebSearchIntentHandler : IIntentHandler
    {
        private readonly GroqService _groq;
        private readonly WebScraperService _scraper;
        private readonly PromptService _promptService;

        public string IntentName => "recherche_web";

        public WebSearchIntentHandler(
            GroqService groq,
            WebScraperService scraper,
            PromptService promptService)
        {
            _groq = groq;
            _scraper = scraper;
            _promptService = promptService;
        }

        public async Task<string> HandleAsync(string userMessage, ILogger logger)
        {
            logger.LogInformation("Recherche web pour: {Message}", userMessage);

            var scraped = await _scraper.GetScrapedAnswerAsync(userMessage);

            if (scraped == null || string.IsNullOrWhiteSpace(scraped.Content))
            {
                logger.LogWarning("Aucun contenu scraped trouvé pour: {Message}", userMessage);
                return "Je n'ai pas trouvé d'information pertinente en ligne. Puis-je vous aider autrement?";
            }

            logger.LogInformation("Contenu trouvé: {Url}, {Length} caractères",
                scraped.Url, scraped.Content.Length);

            string contentToAnalyze = scraped.Content;
            // Limiter la taille pour éviter de dépasser les limites de Groq
            if (contentToAnalyze.Length > 10000)
            {
                contentToAnalyze = contentToAnalyze.Substring(0, 10000) + "...";
            }

            var prompt = _promptService.GetWebSearchPrompt(
                userMessage,
                scraped.Url,
                scraped.Title,
                contentToAnalyze);

            return await _groq.GetCompletionAsync(prompt);
        }
    }
}