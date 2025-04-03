using RootBackend.Explorer.Services;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace RootBackend.Explorer.Skills
{
    public class NavigatorSkill : IRootSkill
    {
        private readonly RootNavigator _navigator;

        public NavigatorSkill()
        {
            _navigator = new RootNavigator();
        }

        public bool CanHandle(string input)
        {
            string[] triggers = new[]
            {
                "va voir", "va sur", "explore", "ouvre", "cherche", "trouve", "regarde", "renseigne-toi"
            };

            return triggers.Any(trigger => input.ToLower().Contains(trigger));
        }

        public async Task<string?> HandleAsync(string input)
        {
            // 1. Essaie d'extraire une URL
            var match = Regex.Match(input, @"https?:\/\/[^\s]+");
            if (match.Success)
            {
                var url = match.Value;
                var content = await _navigator.ExplorePageAsync(url);
                return $"Voici ce que j'ai trouvé sur {url} :\n\n{content}";
            }

            // 2. Sinon : on effectue une recherche web
            var query = Uri.EscapeDataString(input);
            var searchUrl = $"https://html.duckduckgo.com/html/?q={query}";

            var httpClient = new HttpClient();
            var searchResult = await httpClient.GetStringAsync(searchUrl);

            // 3. On extrait un lien pertinent (le premier)
            var linkMatch = Regex.Match(searchResult, @"https?:\/\/[^""]+");

            if (!linkMatch.Success)
                return "Je n'ai pas trouvé de lien pertinent pour cette recherche.";

            var foundUrl = linkMatch.Value;

            var foundContent = await _navigator.ExplorePageAsync(foundUrl);

            return $"J'ai cherché sur Internet avec ta demande : \"{input}\".\nJe suis allée voir : {foundUrl}\n\nVoici ce que j'en ai retenu :\n\n{foundContent}";
        }

    }
}
