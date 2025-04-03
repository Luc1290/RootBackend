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
            // Analyse sémantique préalable pour comprendre l'intention
            var json = await _intentionSkill.HandleAsync(userMessage);
            var parsed = IntentionParser.Parse(json ?? "{}");

            // Logging pour le débogage
            Console.WriteLine($"Intentions détectées : {string.Join(", ", parsed.Intentions)}");
            Console.WriteLine($"Message reçu : '{userMessage}'");

            // Détection des questions sur les activités personnelles
            bool isAboutActivity = userMessage.ToLower().Contains("faire") ||
                                   userMessage.ToLower().Contains("aller") ||
                                   userMessage.ToLower().Contains("vélo") ||
                                   userMessage.ToLower().Contains("velo") ||
                                   userMessage.ToLower().Contains("activité") ||
                                   userMessage.ToLower().Contains("activite");

            bool asksOpinion = userMessage.ToLower().Contains("dois") ||
                               userMessage.ToLower().Contains("devrais") ||
                               userMessage.ToLower().Contains("est-ce que je") ||
                               userMessage.ToLower().Contains("tu penses") ||
                               userMessage.ToLower().Contains("tu crois");

            bool isDecisionQuestion = isAboutActivity && asksOpinion;

            // Si c'est une demande de conseil personnel, envoyer directement au LLM
            if (isDecisionQuestion || parsed.Intentions.Contains("conseil") || parsed.Intentions.Contains("décision"))
            {
                Console.WriteLine("⚡ Détection d'une question de conseil/décision, traitement par le LLM");
                return await _conversationSkill.HandleWithContextAsync(userMessage, parsed);
            }

            // Si le message contient "demain" mais ne parle pas explicitement de météo
            bool containsWeatherTerms = userMessage.ToLower().Contains("météo") ||
                                        userMessage.ToLower().Contains("meteo") ||
                                        userMessage.ToLower().Contains("température") ||
                                        userMessage.ToLower().Contains("temps") ||
                                        userMessage.ToLower().Contains("vent") ||
                                        userMessage.ToLower().Contains("pluie");

            if (userMessage.ToLower().Contains("demain") && !containsWeatherTerms)
            {
                Console.WriteLine("⚡ Détection de 'demain' sans termes météo, traitement par le LLM");
                return await _conversationSkill.HandleWithContextAsync(userMessage, parsed);
            }

            // Vérification des skills spécialisés
            foreach (var skill in _skills)
            {
                // Ignorer l'analyse et la conversation pour éviter les boucles
                if (skill is IntentionSkill || skill is ConversationSkill) continue;

                string skillName = skill.GetType().Name;

                // Vérification améliorée pour chaque skill
                if (skill.CanHandle(userMessage, parsed.Intentions))
                {
                    Console.WriteLine($"✅ Skill trouvé: {skillName}");
                    var response = await skill.HandleAsync(userMessage);

                    if (!string.IsNullOrWhiteSpace(response))
                    {
                        return response;
                    }

                    Console.WriteLine($"⚠️ {skillName} a renvoyé une réponse vide");
                }
                else
                {
                    Console.WriteLine($"❌ {skillName} ne peut pas traiter ce message");
                }
            }

            // Si aucun skill spécialisé n'a pu traiter le message, fallback sur le LLM
            Console.WriteLine("🔄 Aucun skill spécialisé, fallback sur LLM");
            return await _conversationSkill.HandleWithContextAsync(userMessage, parsed);
        }
    }

    // Extension de l'interface pour supporter les intentions
    public static class SkillExtensions
    {
        public static bool CanHandle(this IRootSkill skill, string message, List<string> intentions)
        {
           // Vérification standard du skill
            return skill.CanHandle(message);
        }
    }
}
