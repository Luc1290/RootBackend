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

            // 2. Parcourir les skills
            foreach (var skill in _skills)
            {
                if (skill.CanHandle(message))
                {
                    var response = await skill.HandleWithContextAsync(message, context, userId);
                    if (!string.IsNullOrWhiteSpace(response))
                    {
                        return response;
                    }
                }
            }

            // 3. Fallback si aucune réponse
            return "Je n'ai pas trouvé de réponse adaptée à ta demande, mais je suis toujours en apprentissage !";
        }
    }
}
