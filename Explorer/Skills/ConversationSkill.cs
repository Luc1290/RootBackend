using RootBackend.Services;
using static RootBackend.Explorer.Skills.IntentionSkill;

namespace RootBackend.Explorer.Skills
{
    public class ConversationSkill : IRootSkill
    {
        private readonly GroqService _saba;

        public ConversationSkill(GroqService saba)
        {
            _saba = saba;
        }

        public string SkillName => "ConversationSkill";

        public bool CanHandle(string message)
        {
            // Skill fallback toujours activée si rien d'autre ne prend
            return true;
        }

        public async Task<string?> HandleAsync(string message)
        {
            return await _saba.GetCompletionAsync(message);
        }

        public async Task<string?> HandleWithContextAsync(string message, ParsedIntention context, string userId)
        {
            var prompt = ContextualPromptBuilder.Build(message, context);
            return await _saba.GetCompletionAsync(prompt);
        }
    }
}