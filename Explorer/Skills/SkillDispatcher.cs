using RootBackend.Explorer.Skills;
using RootBackend.Services;
using static RootBackend.Explorer.Skills.IntentionSkill;

namespace RootBackend.Explorer.Skills
{
    public class SkillDispatcher
    {
        private readonly IEnumerable<IRootSkill> _skills;
        private readonly IntentionSkill _intentionSkill;

        public SkillDispatcher(IEnumerable<IRootSkill> skills, IntentionSkill intentionSkill)
        {
            _skills = skills;
            _intentionSkill = intentionSkill;
        }

        public async Task<string> DispatchAsync(string message, string userId = "anonymous")
        {
            // 1. Analyse contextuelle via IntentionSkill
            var context = await _intentionSkill.ParseIntentionAsync(message);

            // 2. Routing explicite selon l’intention
            if (context.Intentions.Contains("recherche") || context.Intentions.Contains("actualité"))
            {
                var navigator = _skills.FirstOrDefault(s => s.SkillName == "NavigatorSkill");
                if (navigator != null)
                {
                    var response = await navigator.HandleWithContextAsync(message, context, userId);
                    if (!string.IsNullOrWhiteSpace(response))
                        return response;
                }
            }
            else
            {
                var conversation = _skills.FirstOrDefault(s => s.SkillName == "ConversationSkill");
                if (conversation != null)
                {
                    var response = await conversation.HandleWithContextAsync(message, context, userId);
                    if (!string.IsNullOrWhiteSpace(response))
                        return response;
                }
            }

            return "Je n'ai pas trouvé de réponse adaptée à ta demande, mais je suis toujours en apprentissage !";
        }

    }
}
