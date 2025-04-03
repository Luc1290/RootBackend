using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RootBackend.Explorer.Services;

namespace RootBackend.Explorer.Skills
{
    public class MeteoSkill : IRootSkill
    {
        private readonly ILogger<MeteoSkill> _logger;
        private readonly IConfiguration _configuration;
        private readonly WebScraperService _scraper;

        public MeteoSkill(ILogger<MeteoSkill> logger, IConfiguration configuration, WebScraperService scraper)
        {
            _logger = logger;
            _configuration = configuration;
            _scraper = scraper;
        }

        public bool CanHandle(string input)
        {
            return Regex.IsMatch(input, @"\bmétéo\b", RegexOptions.IgnoreCase);
        }

        public async Task<string?> HandleAsync(string input)
        {
            var location = ExtractLocation(input) ?? "Millau"; // fallback local 🤠
            var response = await GetWeatherAsync(location);
            return response ?? $"Je n’ai pas pu récupérer la météo pour {location}.";
        }


        private string? ExtractLocation(string input)
        {
            var match = Regex.Match(input, @"(?:à|de|pour)\s+([a-zA-ZÀ-ÿ'\-\s]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private async Task<string?> GetWeatherAsync(string location)
        {
            var (url, content) = await _scraper.ScrapeAsync($"météo {location}");

            if (string.IsNullOrWhiteSpace(content))
                return $"Je n’ai pas trouvé d’information météo pour {location}.";

            return $@"## ☁️ Météo à {char.ToUpper(location[0]) + location.Substring(1)}

🌐 Source : {url}

📄 Résumé :
{content.Substring(0, Math.Min(1200, content.Length))}

---
*Données extraites automatiquement — fiabilité non garantie.*";
        }
    }
}
