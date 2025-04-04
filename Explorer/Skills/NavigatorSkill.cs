using Microsoft.Extensions.Logging;
using RootBackend.Models;
using RootBackend.Services;
using System;
using System.Threading.Tasks;
using static RootBackend.Explorer.Skills.IntentionSkill;

namespace RootBackend.Explorer.Skills
{
    public class NavigatorSkill : IRootSkill
    {
        private readonly WebScraperService _scraper;
        private readonly GroqService _groqService;
        private readonly PromptService _promptService;
        private readonly MessageService _messageService;
        private readonly ILogger<NavigatorSkill> _logger;

        public NavigatorSkill(WebScraperService scraper, GroqService groqService, PromptService promptService, MessageService messageService, ILogger<NavigatorSkill> logger)
        {
            _scraper = scraper;
            _groqService = groqService;
            _promptService = promptService;
            _messageService = messageService;
            _logger = logger;
        }

        public string SkillName => "NavigatorSkill";

        public bool CanHandle(string input)
        {
            return input.Contains("météo", StringComparison.OrdinalIgnoreCase)
                || input.Contains("actualité", StringComparison.OrdinalIgnoreCase)
                || input.Contains("infos", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string?> HandleAsync(string input)
        {
            return await HandleWithContextAsync(input, new ParsedIntention(), "anonymous");
        }

        public async Task<string?> HandleWithContextAsync(string input, ParsedIntention context, string userId)
        {
            _logger.LogInformation("[NAVIGATOR] 🚀 Question : {Input}", input);

            // 🔁 Préférer searchQuery (si dispo) pour un scraping plus pertinent
            var query = !string.IsNullOrWhiteSpace(context.SearchQuery) ? context.SearchQuery : input;
            _logger.LogInformation("[NAVIGATOR] 🔍 Requête utilisée pour scraping : {Query}", query);

            var result = await _scraper.GetScrapedAnswerAsync(query);
            if (result == null || string.IsNullOrWhiteSpace(result.Content))
            {
                _logger.LogWarning("[NAVIGATOR] ❌ Aucun contenu récupéré.");
                return "Je n’ai pas pu obtenir de résultat pour cette recherche.";
            }

            _logger.LogInformation("[NAVIGATOR] 📎 Contenu extrait depuis : {Url}", result.Url);

            // Préparation du prompt Groq via root-nlp
            var prompt = await _promptService.GeneratePromptAsync(
                question: input,
                intention: context.Intentions.FirstOrDefault() ?? "recherche",
                entities: new List<string>(), // Pour l’instant
                url: result.Url,
                content: result.Content
            );

            if (string.IsNullOrWhiteSpace(prompt))
            {
                _logger.LogWarning("[NAVIGATOR] ❌ Prompt non généré.");
                return "Je n’ai pas pu générer de réponse pour cette recherche.";
            }

            var reply = await _groqService.GetCompletionAsync(prompt);
            _logger.LogInformation("[NAVIGATOR] ✅ Réponse Groq : {Reply}", reply);

            await _messageService.SaveUserMessageAsync(input, "navigator-skill", userId);
            await _messageService.SaveBotMessageAsync(reply, "navigator-skill", userId);

            return reply;
        }

    }
}
