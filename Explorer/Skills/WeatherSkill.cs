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
            // Essaye de détecter un nom de ville avec ou sans "à"
            var match = Regex.Match(message, @"(?:à|pour)?\s*([a-zA-ZÀ-ÿ\-']{3,})", RegexOptions.IgnoreCase);
            if (!match.Success) return null;

            var city = match.Groups[1].Value.Trim();
            Console.WriteLine($"Demande météo pour ville: {city}");


            var weather = await _explorer.ExploreWeatherAsync(city);
            if (weather == null)
                return $"🤷 Je ne trouve pas la météo pour {city}.";

            // Crée un prompt pour Groq en injectant les vraies données météo
            // Crée un prompt pour Groq en injectant les vraies données météo
            var prompt = $"""
                Tu es une IA météo. Les données suivantes sont **réelles** et doivent être **reprises telles quelles** :

                Ville : {weather.City}  
                Température : {weather.Temperature}°C  
                Vent : {weather.WindSpeed} km/h  
                Conditions : {weather.Condition}

                Ta mission :
                    - Génère un **paragraphe fluide et agréable**, en Markdown, avec emojis
                    - **N’invente jamais** d’autres chiffres ou conditions
                    - Donne un **petit conseil météo** selon la température

                Sois précis, clair et utile. La météo compte sur toi !
                """;


            var styledReply = await _saba.GetCompletionAsync(prompt);
            return styledReply;
        }

    }
}
