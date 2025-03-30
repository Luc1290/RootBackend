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
            // Plusieurs patterns pour trouver la ville
            var patterns = new[]
            {
        @"à\s+([a-zA-ZÀ-ÿ\- ]+)",          // "à Paris"
        @"de\s+([a-zA-ZÀ-ÿ\- ]+)",         // "de Paris" 
        @"pour\s+([a-zA-ZÀ-ÿ\- ]+)",       // "pour Paris"
        @"dans\s+([a-zA-ZÀ-ÿ\- ]+)",       // "dans Paris"
        @"météo\s+(?:à|de|pour)?\s*([a-zA-ZÀ-ÿ\- ]+)" // "météo Paris"
    };

            string? city = null;
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(message, pattern);
                if (match.Success)
                {
                    city = match.Groups[1].Value.Trim();
                    break;
                }
            }

            // Si aucun pattern ne trouve la ville, extrayez le dernier mot (peut être la ville)
            if (city == null)
            {
                var words = message.Split(new[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 0)
                {
                    city = words[words.Length - 1].Trim();
                }
            }

            // Si toujours pas de ville, informez l'utilisateur
            if (string.IsNullOrWhiteSpace(city))
            {
                return "Je comprends que vous voulez connaître la météo, mais de quelle ville ?";
            }

            Console.WriteLine($"Demande de météo détectée pour la ville: {city}");

            var weather = await _explorer.ExploreWeatherAsync(city);

            if (weather == null)
                return $"Je ne trouve pas la météo pour {city}. Pourriez-vous préciser le nom de la ville ?";

            // Retourner directement la réponse sans passer par Groq pour reformuler
            return $"À {weather.City}, il fait actuellement {weather.Temperature}°C avec un vent de {weather.WindSpeed} km/h.";
        }
    }
}
