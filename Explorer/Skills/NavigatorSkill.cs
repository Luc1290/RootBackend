using RootBackend.Explorer.Services;
using RootBackend.Services;
using System.Text.RegularExpressions;

namespace RootBackend.Explorer.Skills
{
    public class NavigatorSkill : IRootSkill
    {
        private readonly WebScraperService _scraper;
        private readonly GroqService _groq;

        public NavigatorSkill(WebScraperService scraper, GroqService groq)
        {
            _scraper = scraper;
            _groq = groq;
        }

        public bool CanHandle(string input)
        {
            string[] triggers = new[]
            {
                "va voir", "va sur", "explore", "ouvre", "cherche", "trouve", "regarde", "renseigne-toi", "quelle est la météo", "combien coûte", "où acheter"
            };

            return triggers.Any(trigger => input.ToLower().Contains(trigger));
        }

        public async Task<string?> HandleAsync(string input)
        {
            var (url, content) = await _scraper.ScrapeAsync(input);

            if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(url))
                return "Désolé, je n’ai pas réussi à trouver une page pertinente sur ce sujet.";

            var analysis = await _groq.AnalyzeHtmlAsync(content, input);

            return $"🔍 J’ai exploré le web pour répondre à ta question : \"{input}\".\n" +
                   $"📎 Source : {url}\n\n" +
                   $"🧠 Résumé :\n{analysis}";
        }
    }
}
