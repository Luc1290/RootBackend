using System;
using Microsoft.Extensions.Configuration;

namespace RootBackend.Services
{
    public class PromptService
    {
        private readonly IConfiguration _configuration;

        public PromptService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetEntityExtractionPrompt(string entityType)
        {
            return $"Tu es un assistant spécialisé en extraction d'entités. Tu dois extraire uniquement le {entityType} mentionné dans le message de l'utilisateur. Réponds juste avec le nom de l'entité, sans phrase, sans formatage.";
        }

        public string GetHtmlAnalysisPrompt(string htmlContent, string userQuery)
        {
            // Limiter la taille pour éviter de dépasser les limites
            if (htmlContent.Length > 10000)
            {
                htmlContent = htmlContent.Substring(0, 10000) + "...";
            }

            return $"""
Tu es un assistant intelligent. Voici le contenu extrait d'une page web (limité à 10 000 caractères). Résume les informations utiles en lien avec la question suivante.

# QUESTION UTILISATEUR
{userQuery}

# CONTENU DE LA PAGE
{htmlContent}

# RÉPONSE ATTENDUE
""";
        }

        public string GetConversationPrompt(string userMessage)
        {
            return $@"
L'utilisateur dit: '{userMessage}'

Réponds de façon naturelle, utile et concise. Sois cordial mais pas trop familier.
";
        }

        public string GetCodeGenerationPrompt(string userMessage)
        {
            return $@"
L'utilisateur demande du code pour: '{userMessage}'

Fournis le code demandé avec des explications claires. Si possible:
1. Explique brièvement la logique globale
2. Commente les parties importantes du code
3. Fournis des instructions d'utilisation si nécessaire

Si la demande n'est pas claire, propose plusieurs solutions possibles.
";
        }

        public string GetWebSearchPrompt(string userMessage, string url, string title, string content)
        {
            return @$"
Voici une information trouvée en ligne en réponse à la question: '{userMessage}'

SOURCE: {url}
TITRE: {title}

CONTENU:
{content}

Réponds à la question de manière concise et informative, en te basant uniquement sur les informations ci-dessus.
Si les informations ne permettent pas de répondre à la question, indique-le clairement.
";
        }
    }
}