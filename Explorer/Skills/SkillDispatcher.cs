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
            Console.WriteLine($"📌 Dispatching message: \"{message}\" pour UserId: {userId}");

            // 1. Analyse contextuelle via IntentionSkill
            Console.WriteLine("🔍 Analyse contextuelle...");
            var context = await _intentionSkill.ParseIntentionAsync(message);
            Console.WriteLine($"🎯 Intention détectée : {string.Join(", ", context.Intentions)}");

            // 2. Routing explicite selon l’intention
            if (context.Intentions.Contains("recherche") || context.Intentions.Contains("actualité"))
            {
                Console.WriteLine("🧭 Redirection vers NavigatorSkill...");
                var navigator = _skills.FirstOrDefault(s => s.SkillName == "NavigatorSkill");
                if (navigator != null)
                {
                    var response = await navigator.HandleWithContextAsync(message, context, userId);
                    Console.WriteLine($"📡 Réponse NavigatorSkill : {response}");
                    if (!string.IsNullOrWhiteSpace(response))
                        return response;
                }
                else
                {
                    Console.WriteLine("❌ NavigatorSkill non trouvé !");
                }
            }
            else
            {
                Console.WriteLine("💬 Redirection vers ConversationSkill...");
                var conversation = _skills.FirstOrDefault(s => s.SkillName == "ConversationSkill");
                if (conversation != null)
                {
                    var response = await conversation.HandleWithContextAsync(message, context, userId);
                    Console.WriteLine($"💡 Réponse ConversationSkill : {response}");
                    if (!string.IsNullOrWhiteSpace(response))
                        return response;
                }
                else
                {
                    Console.WriteLine("❌ ConversationSkill non trouvé !");
                }
            }

            Console.WriteLine("❓ Aucune réponse trouvée par les skills.");
            return "Je n'ai pas trouvé de réponse adaptée à ta demande, mais je suis toujours en apprentissage !";
        }


    }
}
