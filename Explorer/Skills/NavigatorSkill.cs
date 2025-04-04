using Microsoft.Extensions.Logging;
using RootBackend.Explorer.Services;
using RootBackend.Models;
using RootBackend.Services;
using System.Diagnostics;
using static RootBackend.Explorer.Skills.IntentionSkill;

namespace RootBackend.Explorer.Skills
{
    public class NavigatorSkill : IRootSkill
    {
        private readonly WebScraperService _scraper;
        private readonly GroqService _groqService;
        private readonly MessageService _messageService;
        private readonly ILogger<NavigatorSkill> _logger;

        public NavigatorSkill(WebScraperService scraper, GroqService groqService, MessageService messageService, ILogger<NavigatorSkill> logger)
        {
            _scraper = scraper;
            _groqService = groqService;
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
            _logger.LogInformation("[SCRAPER] 🔍 Requête reçue pour : \"{Input}\"", input);

            var (url, pageContent) = await _scraper.ScrapeAsync(input, "navigator");

            if (string.IsNullOrWhiteSpace(pageContent) || pageContent == "Aucun contenu." || pageContent.Contains("Erreur"))
            {
                _logger.LogWarning("[SCRAPER] ❌ Aucun contenu valide récupéré.");
                return "Je n’ai pas pu obtenir de résultat pour cette recherche.";
            }

            _logger.LogInformation("[SCRAPER] 📄 Page extraite depuis : {Url}", url);

            string prompt = $@"
Tu es un assistant intelligent.
Voici le contenu d'une page web lié à la question : 
            { input}

=== DÉBUT DU CONTENU SCRAPPÉ ===
{ pageContent}
=== FIN DU CONTENU ===

Ta tâche est de répondre à l'utilisateur en te basant uniquement sur ce contenu. Si aucune réponse n’est trouvée, dis-le simplement.
";

            var reply = await _groqService.AnalyzeHtmlAsync(pageContent, input);
            _logger.LogInformation("[SCRAPER] ✅ Réponse IA : {Reply}", reply);

            var userMessage = new MessageLog
            {
                Id = Guid.NewGuid(),
                Content = input,
                Sender = "user",
                Source = "navigator-skill",
                Timestamp = DateTime.UtcNow,
                Type = "query",
                UserId = userId
            };

            await _messageService.SaveUserMessageAsync(userMessage.Content, userMessage.Source, userMessage.UserId);
            await _messageService.SaveBotMessageAsync(reply, "navigator-skill", userId);

            return reply;
        }
    }
}
