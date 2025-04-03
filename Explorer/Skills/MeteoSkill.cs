using System.Text.RegularExpressions;
using Microsoft.Playwright;
using RootBackend.Explorer.Skills;

namespace RootBackend.Explorer.Skills
{
    public class MeteoSkill : IRootSkill
    {
        private readonly ILogger<MeteoSkill> _logger;
        private readonly IConfiguration _configuration;

        public MeteoSkill(ILogger<MeteoSkill> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public bool CanHandle(string message)
        {
            // Détection des requêtes météo
            string messageLower = message.ToLower();

            // Recherche de mots-clés liés à la météo
            string[] meteoKeywords = new[]
            {
                "météo", "meteo", "temps", "température", "temperature",
                "climat", "pluie", "soleil", "nuage", "orage", "vent", "neige",
                "fait-il", "fait il", "quel temps"
            };

            // Vérifiez si un des mots-clés est présent
            return meteoKeywords.Any(keyword => messageLower.Contains(keyword));
        }

        public async Task<string?> HandleAsync(string message)
        {
            try
            {
                // Extraction du lieu depuis le message
                string? location = ExtractLocation(message);

                if (string.IsNullOrEmpty(location))
                {
                    return "Je n'ai pas pu déterminer le lieu pour lequel vous souhaitez connaître la météo. Pourriez-vous préciser?";
                }

                // Récupérer les informations météo avec Playwright
                var weatherData = await GetWeatherWithPlaywrightAsync(location);

                if (weatherData == null)
                {
                    return $"Désolé, je n'ai pas pu obtenir les informations météo pour {location}.";
                }

                return weatherData;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur dans MeteoSkill: {ex.Message}");
                return "Je suis désolé, j'ai rencontré un problème pour obtenir les informations météo. Veuillez réessayer plus tard.";
            }
        }

        private string? ExtractLocation(string message)
        {
            // Expressions régulières pour extraire le lieu
            var patterns = new[]
            {
                @"météo (?:à|a|pour|de) ([A-Za-zÀ-ÖØ-öø-ÿ\s-]+)(?:\?|\.|\s|$)",
                @"meteo (?:à|a|pour|de) ([A-Za-zÀ-ÖØ-öø-ÿ\s-]+)(?:\?|\.|\s|$)",
                @"temps (?:à|a|pour|de) ([A-Za-zÀ-ÖØ-öø-ÿ\s-]+)(?:\?|\.|\s|$)",
                @"quel(?:le)? (?:météo|meteo|temps) (?:à|a|pour|de|fait[- ]il (?:à|a)) ([A-Za-zÀ-ÖØ-öø-ÿ\s-]+)(?:\?|\.|\s|$)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(message.ToLower(), pattern);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            // Si aucun pattern ne correspond, essayer avec une approche plus simple
            string[] messageParts = message.ToLower().Split(new[] { ' ', '?', '.' }, StringSplitOptions.RemoveEmptyEntries);
            int index = -1;

            // Chercher des prépositions comme "à", "a", "pour", "de"
            foreach (var preposition in new[] { "à", "a", "pour", "de" })
            {
                index = Array.IndexOf(messageParts, preposition);
                if (index >= 0 && index < messageParts.Length - 1)
                {
                    return messageParts[index + 1].Trim();
                }
            }

            return null;
        }

        private async Task<string?> GetWeatherWithPlaywrightAsync(string location)
        {
            // Nous allons utiliser Playwright pour extraire les données de météo-france
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var page = await browser.NewPageAsync();

            try
            {
                // Accéder à météo-france avec la recherche de la ville
                await page.GotoAsync($"https://meteofrance.com/recherche/resultats?query={Uri.EscapeDataString(location)}");

                // Attendre que les résultats apparaissent
                await page.WaitForSelectorAsync(".search-results-content .card-link", new PageWaitForSelectorOptions { Timeout = 10000 });

                // Cliquer sur le premier résultat
                await page.ClickAsync(".search-results-content .card-link");

                // Attendre que la page de détails se charge
                await page.WaitForSelectorAsync(".day-summary-temperature", new PageWaitForSelectorOptions { Timeout = 10000 });

                // Extraire les données
                var temperature = await page.TextContentAsync(".day-summary-temperature");
                var description = await page.TextContentAsync(".day-summary-label");
                var windInfo = await page.TextContentAsync(".day-summary-wind");

                // Récupérer les prévisions pour les prochains jours
                var forecasts = await page.EvaluateAsync<string[]>(@"
                    Array.from(document.querySelectorAll('.forecast-day')).map(day => {
                        const date = day.querySelector('.forecast-day-date').textContent;
                        const weather = day.querySelector('.forecast-day-weather').textContent;
                        const temp = day.querySelector('.forecast-day-temperature').textContent;
                        return `${date}: ${weather}, ${temp}`;
                    })
                ");

                // Formatage de la réponse
                string forecastsText = string.Join("\n- ", forecasts);

                return $@"## Météo à {char.ToUpper(location[0]) + location.Substring(1)}

### Conditions météorologiques actuelles
- **Température** : {temperature?.Trim() ?? "Non disponible"}
- **Conditions** : {description?.Trim() ?? "Non disponible"}
- **Vent** : {windInfo?.Trim() ?? "Information non disponible"}

### Prévisions pour les prochains jours
- {forecastsText}

### Conseils
- **Vêtements recommandés** : {GetClothingRecommendation(temperature)}
- **Activités recommandées** : {GetActivityRecommendation(description)}

Ces informations sont extraites de Météo-France et sont régulièrement mises à jour.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur lors du scraping météo: {ex.Message}");

                // Essayer une source alternative (météo gratuite)
                try
                {
                    await page.GotoAsync($"https://www.meteociel.fr/previsions/{Uri.EscapeDataString(location)}.htm");

                    await page.WaitForSelectorAsync(".bloc_jour", new PageWaitForSelectorOptions { Timeout = 10000 });

                    var temperature = await page.TextContentAsync(".temperature");
                    var description = await page.TextContentAsync(".description");

                    return $@"## Météo à {char.ToUpper(location[0]) + location.Substring(1)}

### Conditions météorologiques actuelles
- **Température** : {temperature?.Trim() ?? "Non disponible"}
- **Conditions** : {description?.Trim() ?? "Non disponible"}

### Conseils
- **Vêtements recommandés** : {GetClothingRecommendation(temperature)}
- **Activités recommandées** : {GetActivityRecommendation(description)}

Ces informations sont extraites de Meteociel et sont régulièrement mises à jour.";
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError($"Erreur lors du scraping source alternative: {fallbackEx.Message}");
                    return null;
                }
            }
        }

        private string GetClothingRecommendation(string? temperatureText)
        {
            if (string.IsNullOrEmpty(temperatureText))
                return "Vérifiez les conditions avant de sortir";

            // Extraire la valeur numérique de la température
            var match = Regex.Match(temperatureText, @"(\d+)");
            if (!match.Success)
                return "Habillez-vous en fonction de la saison";

            if (int.TryParse(match.Groups[1].Value, out int temperature))
            {
                if (temperature > 25)
                    return "Vêtements légers, n'oubliez pas votre protection solaire";
                else if (temperature > 15)
                    return "Tenue légère avec une couche supplémentaire pour le soir";
                else if (temperature > 5)
                    return "Vêtements chauds, emportez un imperméable si des précipitations sont prévues";
                else
                    return "Vêtements d'hiver, plusieurs couches et protection contre le froid";
            }

            return "Adaptez votre tenue en fonction des conditions extérieures";
        }

        private string GetActivityRecommendation(string? descriptionText)
        {
            if (string.IsNullOrEmpty(descriptionText))
                return "Consultez les prévisions détaillées avant de planifier vos activités";

            descriptionText = descriptionText.ToLower();

            if (descriptionText.Contains("pluie") || descriptionText.Contains("pluvieux") || descriptionText.Contains("averse"))
                return "Activités en intérieur recommandées";
            else if (descriptionText.Contains("soleil") || descriptionText.Contains("ensoleillé") || descriptionText.Contains("dégagé"))
                return "Parfait pour des activités de plein air comme la randonnée ou des sorties au parc";
            else if (descriptionText.Contains("neige") || descriptionText.Contains("gel") || descriptionText.Contains("verglas"))
                return "Limitez votre temps à l'extérieur. Profitez d'activités en intérieur";
            else
                return "Bon moment pour des activités extérieures modérées, prévoyez des vêtements adaptés";
        }
    }
}