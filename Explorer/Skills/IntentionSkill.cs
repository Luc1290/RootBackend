using System.Text.Json.Serialization;
using RootBackend.Services;

namespace RootBackend.Explorer.Skills
{
    public class IntentionSkill : IRootSkill
    {
        private readonly NlpService _nlpService;

        public IntentionSkill(GroqService groq, NlpService nlpService)
        {
            _nlpService = nlpService;
        }

        public string SkillName => "IntentionSkill";

        public bool CanHandle(string message)
        {
            return true;
        }

        public async Task<string?> HandleAsync(string message)
        {
            var parsed = await ParseIntentionAsync(message);

            var summary = $"**Intentions principales** : {string.Join(", ", parsed.Intentions)}\n\n" +
                          $"**Sous-texte** : {parsed.SousTexte}";

            return summary;
        }

        public async Task<string?> HandleWithContextAsync(string message, ParsedIntention context, string userId)
        {
            return await HandleAsync(message);
        }

        public async Task<ParsedIntention> ParseIntentionAsync(string message)
        {
            var parsed = new ParsedIntention();

            var nlpResult = await _nlpService.AnalyzeAsync(message);
            if (nlpResult == null)
                return parsed;

            parsed.Intentions.Add(nlpResult.Intention);
            parsed.SousTexte = $"Mots clés : {string.Join(", ", nlpResult.Entities)}";

            return parsed;
        }

        public class ParsedIntention
        {
            [JsonPropertyName("intentions")]
            public List<string> Intentions { get; set; } = new();

            [JsonPropertyName("sousTexte")]
            public string SousTexte { get; set; } = string.Empty;

            [JsonPropertyName("searchQuery")]
            public string SearchQuery { get; set; } = string.Empty;

        }
    }
}