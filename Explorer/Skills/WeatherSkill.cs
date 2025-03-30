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
            return msg.Contains("meteo") || msg.Contains("quel temps") || msg.Contains("temperature") || msg.Contains("il fait combien") || msg.Contains("temps qu'il fait");
        }

        // Remplacer la méthode HandleAsync dans WeatherSkill.cs
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

            var weather = await _explorer.ExploreWeatherAsync(city);
            if (weather == null)
            {
                Console.WriteLine($"❌ Ville non trouvée par l'API : {city}");
                return $"Je n'ai pas réussi à trouver la météo pour **{city}**. Vérifie l'orthographe ou essaie une grande ville.";
            }

            // Le reste du code reste inchangé
            string conseil = weather.Temperature switch
            {
                <= 5 => "🥶 Il fait très froid, pense à bien te couvrir !",
                <= 15 => "🧥 Un pull ou une veste sera parfait.",
                <= 25 => "😎 Une température agréable pour sortir.",
                _ => "🥵 Il fait bien chaud, pense à t'hydrater et à rester au frais."
            };

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

            return $"""
    🌍 **Ville** : {weather.City}
    🌡️ **Température** : {weather.Temperature}°C
    💨 **Vent** : {weather.WindSpeed} km/h
    📋 **Conditions** : {weather.Condition}

    {description}

    🔎 {conseil}

    👉 Si tu veux plus d'infos, n'hésite pas à demander la météo d'une autre ville ou un conseil vestimentaire !
    """;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
            return new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
        }
    }
}
