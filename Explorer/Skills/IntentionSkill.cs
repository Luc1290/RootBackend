using System.Text.Json;
using System.Text.Json.Serialization;
using RootBackend.Services;

namespace RootBackend.Explorer.Skills
{
    public class IntentionSkill : IRootSkill
    {
        private readonly GroqService _groq;

        public IntentionSkill(GroqService groq)
        {
            _groq = groq;
        }

        public string SkillName => "IntentionSkill";

        public bool CanHandle(string message)
        {
            // Toujours activé en tâche d'analyse
            return true;
        }

        public async Task<string?> HandleAsync(string message)
        {
            var parsed = await ParseIntentionAsync(message);

            var summary = $"**Intentions principales** : {string.Join(", ", parsed.Intentions)}\n\n" +
                          $"**Sous-texte** : {parsed.SousTexte}\n\n" +
                          $"**\u00c9motion per\u00e7ue** : *{parsed.Emotion}*\n\n" +
                          $"**Tonalit\u00e9** : *{parsed.Tonalite}*";

            return summary;
        }

        public async Task<string?> HandleWithContextAsync(string message, ParsedIntention context, string userId)
        {
            // Optionnellement, ici tu pourrais enrichir ou revalider le contexte
            return await HandleAsync(message);
        }

        public async Task<ParsedIntention> ParseIntentionAsync(string message) // Changement de private à public
        {
            var prompt = BuildStructuredPrompt(message);
            var response = await _groq.GetCompletionAsync(prompt);
            return IntentionParser.Parse(response);
        }

        private string BuildStructuredPrompt(string message)
        {
            return $@"
Tu es une intelligence avancé doté d'une capacité de compréhension contextuelle supérieure.
Analyse le message de l'utilisateur et retourne uniquement un JSON strict au format suivant :

{{
  ""intentions"": [""information"", ""décision""],
  ""sousTexte"": ""..."",
  ""emotion"": ""..."",
  ""tonalite"": ""...""
}}

Message utilisateur :
""{message}""
";
        }

        public static class IntentionParser
        {
            public static ParsedIntention Parse(string rawJson)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<ParsedIntention>(rawJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return result ?? new ParsedIntention();
                }
                catch
                {
                    return new ParsedIntention();
                }
            }
        }

        public class ParsedIntention
        {
            [JsonPropertyName("intentions")]
            public List<string> Intentions { get; set; } = new();

            [JsonPropertyName("sousTexte")]
            public string SousTexte { get; set; } = string.Empty;

            [JsonPropertyName("emotion")]
            public string Emotion { get; set; } = string.Empty;

            [JsonPropertyName("tonalite")]
            public string Tonalite { get; set; } = string.Empty;
        }
    }

}
