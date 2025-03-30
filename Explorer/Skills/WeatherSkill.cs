using System.Text.RegularExpressions;
using RootBackend.Explorer.Models;
using RootBackend.Explorer.Services;
using RootBackend.Explorer.Skills;
using RootBackend.Services; // Pour GroqService

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
            var msg = message.ToLower();
            return msg.Contains("météo") ||
                   msg.Contains("temps qu'il fait") ||
                   msg.Contains("il fait combien") ||
                   msg.Contains("température") ||
                   msg.Contains("quel temps");
        }

        public async Task<string?> HandleAsync(string message)
        {
            // Détection souple du nom de ville
            var match = Regex.Match(message, @"(?:à|pour)?\s*([A-ZÂ-ÿ][a-zà-ÿ\-']{2,}(?:\s+[A-ZÂ-ÿ]?[a-zà-ÿ\-']+)*)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                Console.WriteLine("❌ Aucune ville détectée dans le message.");
                return "Je n’ai pas bien compris de quelle ville tu parles. Tu peux reformuler avec le nom d’une ville ?";
            }

            var city = match.Groups[1].Value.Trim();
            Console.WriteLine($"🌍 Demande météo pour ville : {city}");

            var weather = await _explorer.ExploreWeatherAsync(city);
            if (weather == null)
            {
                Console.WriteLine($"❌ Ville non trouvée par l’API : {city}");
                return $"Je n’ai pas réussi à trouver la météo pour **{city}**. Vérifie l’orthographe ou essaie une grande ville.";
            }

            // Conseil météo selon température
            string conseil = weather.Temperature switch
            {
                <= 5 => "🥶 Il fait très froid, pense à bien te couvrir !",
                <= 15 => "🧥 Un pull ou une veste sera parfait.",
                <= 25 => "😎 Une température agréable pour sortir.",
                _ => "🥵 Il fait bien chaud, pense à t’hydrater et à rester au frais."
            };

            // Reformulation des conditions météo
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
            À **{weather.City}**, il fait actuellement **{weather.Temperature}°C**, avec un vent de **{weather.WindSpeed} km/h**.  
            {description}.  
            {conseil}
            """;
        }


    }
}
