using RootBackend.Core;
using RootBackend.Explorer.Skills;
using static RootBackend.Explorer.Skills.IntentionSkill;

namespace RootBackend.Services
{
    public static class ContextualPromptBuilder
    {
        public static string Build(string userMessage, ParsedIntention context)
        {
            var basePrompt = RootIdentity.BuildPrompt(userMessage);

            var metadata = $"""

# CONTEXTE D’INTENTION
- Intentions : {string.Join(", ", context.Intentions)}
- Émotion perçue : {context.Emotion}
- Tonalité : {context.Tonalite}
- Sous-texte interprété : {context.SousTexte}
""";

            return basePrompt + metadata;
        }
    }
}
