using RootBackend.Explorer.Skills;
using static RootBackend.Explorer.Skills.IntentionSkill;
using RootBackend.Services;

namespace RootBackend.Services
{
    public class SkillDispatcher
    {
        private readonly IEnumerable<IRootSkill> _skills;
        private readonly IntentionSkill _intentionSkill;
        private readonly ConversationSkill _conversationSkill;

        public SkillDispatcher(IEnumerable<IRootSkill> skills, IntentionSkill intentionSkill, ConversationSkill conversationSkill)
        {
            _skills = skills;
            _intentionSkill = intentionSkill;
            _conversationSkill = conversationSkill;
        }

        public async Task<string?> DispatchAsync(string userMessage)
        {
            // Analyse sémantique préalable
            var json = await _intentionSkill.HandleAsync(userMessage);
            var parsed = IntentionParser.Parse(json ?? "{}");

            foreach (var skill in _skills)
            {
                if (skill is IntentionSkill || skill is ConversationSkill) continue; // Ne pas réappeler l'analyse elle-même ou le fallback ici

                // Nouvelle version de CanHandle enrichie avec intentions
                if (skill.CanHandle(userMessage, parsed.Intentions))
                {
                    var response = await skill.HandleAsync(userMessage);
                    if (!string.IsNullOrWhiteSpace(response))
                    {
                        return response;
                    }
                }
            }

            // Fallback sur ConversationSkill avec conscience du contexte
            return await _conversationSkill.HandleWithContextAsync(userMessage, parsed);
        }
    }

    // Extension de l'interface pour supporter les intentions
    public static class SkillExtensions
    {
        public static bool CanHandle(this IRootSkill skill, string message, List<string> intentions)
        {
            // Implémentation personnalisable par skill
            if (skill is WeatherSkill && intentions.Contains("information") && message.ToLower().Contains("météo"))
                return true;

            if (skill.CanHandle(message)) // fallback au comportement normal
                return true;

            return false;
        }
    }
}
