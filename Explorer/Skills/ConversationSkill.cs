using RootBackend.Services;
using static RootBackend.Explorer.Skills.IntentionSkill;

namespace RootBackend.Explorer.Skills
{
    public partial class ConversationSkill : IRootSkill
    {
        private readonly GroqService _saba;

        public ConversationSkill(GroqService saba)
        {
            _saba = saba;
        }

        // Cette méthode doit retourner true uniquement si la requête semble être 
        // une conversation générale plutôt qu'une demande spécifique
        public bool CanHandle(string message)
        {
            // Vous pourriez ajouter une logique plus sophistiquée ici
            // Pour l'instant, retournons simplement true car nous voulons qu'il s'agisse du fallback
            return true;
        }

        public async Task<string?> HandleWithContextAsync(string message, ParsedIntention context)
        {
            var prompt = ContextualPromptBuilder.Build(message, context);
            return await _saba.GetCompletionAsync(prompt);
        }

        // Implémentation de la méthode HandleAsync requise par l'interface IRootSkill
        public async Task<string?> HandleAsync(string message)
        {
            // Vous pouvez ajouter une logique ici pour gérer le message sans contexte
            return await _saba.GetCompletionAsync(message);
        }
    }
}
