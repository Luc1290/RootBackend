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
            var match = Regex.Match(message, @"à\s+([a-zA-ZÀ-ÿ\- ]+)");
            if (!match.Success) return null;

            var city = match.Groups[1].Value.Trim();
            Console.WriteLine($"Demande météo pour ville: {city}");

            var weather = await _explorer.ExploreWeatherAsync(city);

            if (weather == null)
                return $"Je ne trouve pas la météo pour {city}.";

            // Retourner directement la réponse formatée sans passer par Groq
            return $"À {weather.City}, il fait actuellement {weather.Temperature}°C avec un vent de {weather.WindSpeed} km/h. {weather.Condition}";
        }
    }
}
