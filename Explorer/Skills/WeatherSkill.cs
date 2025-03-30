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
            return msg.Contains("météo") || msg.Contains("temps qu’il fait") || msg.Contains("il fait combien");
        }

        public async Task<string?> HandleAsync(string message)
        {
            var match = Regex.Match(message, @"à\s+([a-zA-ZÀ-ÿ\- ]+)");
            if (!match.Success) return null;

            var city = match.Groups[1].Value.Trim();
            var weather = await _explorer.ExploreWeatherAsync(city);

            if (weather == null)
                return $"Je ne trouve pas la météo pour {city}.";

            var raw = $"À {weather.City}, il fait {weather.Temperature}°C avec un vent de {weather.WindSpeed} km/h.";

            try
            {
                var prompt = $"Voici une info météo à reformuler élégamment et la plus complète pour l’utilisateur : {raw}";
                var reformulated = await _saba.GetCompletionAsync(prompt);
                return reformulated ?? raw;
            }
            catch
            {
                return raw;
            }

        }
    }
}
