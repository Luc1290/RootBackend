using static RootBackend.Explorer.Skills.IntentionSkill;

namespace RootBackend.Explorer.Skills
{
    public interface IRootSkill
    {
        /// <summary>
        /// Nom de la skill (utile pour les logs, le debug, l’interface admin)
        /// </summary>
        string SkillName { get; }

        /// <summary>
        /// Indique si la skill peut gérer le message donné.
        /// </summary>
        bool CanHandle(string message);

        /// <summary>
        /// Gère un message sans contexte enrichi (fallback ou usage basique).
        /// </summary>
        Task<string?> HandleAsync(string message);

        /// <summary>
        /// Gère un message avec intention, émotion, tonalité, etc.
        /// Par défaut, retourne null si non implémentée.
        /// </summary>
        Task<string?> HandleWithContextAsync(string message, ParsedIntention context, string userId)
            => Task.FromResult<string?>(null);
    }
}
