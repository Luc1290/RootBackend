using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using RootBackend.Explorer.Models;
using RootBackend.Explorer.Services;
using RootBackend.Explorer.Skills;
using RootBackend.Services;

namespace RootBackend.Explorer.Skills
{
    public class WeatherSkill : IRootSkill
    {
        private readonly WeatherExplorer _explorer;
        private readonly GroqService _saba;

        public WeatherSkill(WeatherExplorer explorer, GroqService saba)
        {
            _explorer = explorer;
            _saba = saba;
        }

        public bool CanHandle(string message)
        {
            var msg = RemoveDiacritics(message).ToLowerInvariant();

            // Liste de termes explicitement liés à la météo
            bool hasMeteologicalTerms = msg.Contains("meteo") ||
                           msg.Contains("quel temps") ||
                           msg.Contains("temperature") ||
                           msg.Contains("il fait combien") ||
                           msg.Contains("temps qu'il fait") ||
                           msg.Contains("prevision");

            // Mots génériques qui ne devraient déclencher le skill que s'ils sont accompagnés de termes météo
            bool hasContextTerms = (msg.Contains("jours") ||
                                  msg.Contains("semaine") ||
                                  msg.Contains("prochains jours") ||
                                  msg.Contains("demain")) && hasMeteologicalTerms;

            // Récupérer les questions sur la météo explicites OU les questions génériques qui contiennent des termes météo
            return hasMeteologicalTerms || hasContextTerms;
        }
        
        public async Task<string?> HandleAsync(string message)
        {
            // 🔠 Mode sans faute : nettoyer le message utilisateur
            message = RemoveDiacritics(message).ToLowerInvariant();

            // Utiliser Groq pour extraire le nom de la ville
            var city = await _saba.ExtractEntityAsync(message, "nom de ville");

            if (string.IsNullOrWhiteSpace(city))
            {
                Console.WriteLine("❌ Aucune ville détectée dans le message.");
                return "Je n'ai pas bien compris de quelle ville tu parles. Tu peux reformuler avec le nom d'une ville ?";
            }

            Console.WriteLine($"🌍 Demande météo pour ville : {city}");

            // Toujours inclure les prévisions
            var weather = await _explorer.ExploreWeatherAsync(city, true);
            if (weather == null)
            {
                Console.WriteLine($"❌ Ville non trouvée par l'API : {city}");
                return $"Je n'ai pas réussi à trouver la météo pour **{city}**. Vérifie l'orthographe ou essaie une grande ville.";
            }

            // Toujours afficher la météo actuelle + prévisions
            return FormatCompleteWeatherResponse(weather);
        }

        private string FormatCompleteWeatherResponse(WeatherResult weather)
        {
            var sb = new StringBuilder();

            // Conseil selon la température actuelle
            string conseil = weather.Temperature switch
            {
                <= 5 => "🥶 Il fait très froid, pense à bien te couvrir !",
                <= 15 => "🧥 Un pull ou une veste sera parfait.",
                <= 25 => "😎 Une température agréable pour sortir.",
                _ => "🥵 Il fait bien chaud, pense à t'hydrater et à rester au frais."
            };

            // Description des conditions actuelles
            string condition = weather.Condition.ToLower();
            string description = condition switch
            {
                var c when c.Contains("ensoleillé") || c.Contains("dégagé") => "Le ciel est parfaitement dégagé ☀️",
                var c when c.Contains("nuageux") => "Le ciel est partiellement couvert ☁️",
                var c when c.Contains("pluie") || c.Contains("averses") => "Il pleut actuellement 🌧️",
                var c when c.Contains("orage") => "Des orages sont en cours ⚡",
                var c when c.Contains("neige") => "La neige tombe sur la ville ❄️",
                var c when c.Contains("brouillard") => "Un brouillard épais limite la visibilité 🌫️",
                _ => $"Conditions actuelles : *{weather.Condition}*"
            };

            // Météo actuelle
            sb.AppendLine($"🌍 **Ville** : {weather.City}");
            sb.AppendLine($"🌡️ **Température** : {weather.Temperature}°C");
            sb.AppendLine($"💨 **Vent** : {weather.WindSpeed} km/h");
            sb.AppendLine($"📋 **Conditions** : {weather.Condition}");
            sb.AppendLine();
            sb.AppendLine(description);
            sb.AppendLine();
            sb.AppendLine($"🔎 {conseil}");
            sb.AppendLine();

            // Prévisions pour les prochains jours (si disponibles)
            if (weather.Forecasts != null && weather.Forecasts.Count > 0)
            {
                sb.AppendLine($"📅 **Prévisions pour les prochains jours** :");

                foreach (var forecast in weather.Forecasts.Take(7)) // Limiter à 7 jours
                {
                    string dayName = forecast.Date.ToString("dddd", new CultureInfo("fr-FR"));
                    dayName = char.ToUpper(dayName[0]) + dayName.Substring(1); // Première lettre en majuscule

                    string emoji = forecast.Condition.ToLower() switch
                    {
                        var c when c.Contains("ensoleillé") || c.Contains("dégagé") => "☀️",
                        var c when c.Contains("nuageux") => "☁️",
                        var c when c.Contains("pluie") || c.Contains("averses") => "🌧️",
                        var c when c.Contains("orage") => "⚡",
                        var c when c.Contains("neige") => "❄️",
                        var c when c.Contains("brouillard") => "🌫️",
                        _ => "🌤️"
                    };

                    sb.AppendLine($"- **{dayName} {forecast.Date:dd/MM}** {emoji} : {forecast.MinTemperature}°C à {forecast.MaxTemperature}°C, {forecast.Condition}");
                    if (forecast.PrecipitationProbability > 0)
                    {
                        sb.AppendLine($"  💧 Probabilité de précipitations : {forecast.PrecipitationProbability}%");
                    }
                }
            }

            sb.AppendLine("\n👉 Si tu veux plus d'infos, n'hésite pas à demander la météo d'une autre ville ou un conseil vestimentaire !");

            return sb.ToString();
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
            return new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
        }
    }
}