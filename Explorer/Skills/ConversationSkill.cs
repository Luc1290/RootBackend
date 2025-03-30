using RootBackend.Services;

namespace RootBackend.Explorer.Skills
{
    public class ConversationSkill : IRootSkill
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

        public async Task<string?> HandleAsync(string message)
        {
            // Utiliser directement le service Groq existant
            return await _saba.GetCompletionAsync(message);
        }
    }
}