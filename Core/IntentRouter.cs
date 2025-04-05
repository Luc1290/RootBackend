using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RootBackend.Models;
using RootBackend.Services;

namespace RootBackend.Core
{
    public static class IntentRouter
    {
        public static async Task<string> HandleAsync(
            NlpResponse nlp,
            string userMessage,
            GroqService groq,
            WebScraperService scraper,
            ILogger logger)
        {
            if (nlp == null)
            {
                logger.LogWarning("NLP response null, fallback à l'intention 'discussion'");
                nlp = new NlpResponse { Intent = "discussion" };
            }

            logger.LogInformation("Traitement de l'intention: {Intent} pour le message: {Message}",
                nlp.Intent, userMessage);

            try
            {
                switch (nlp.Intent)
                {
                    case "recherche_web":
                        return await HandleWebSearch(userMessage, groq, scraper, logger);

                    case "generation_code":
                        return await HandleCodeGeneration(userMessage, groq, logger);

                    case "generation_image":
                        return await HandleImageGeneration(userMessage, logger);

                    case "discussion":
                    default:
                        return await HandleConversation(userMessage, groq, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur lors du traitement de l'intention {Intent}: {Message}",
                    nlp.Intent, ex.Message);

                // Toujours retourner une réponse, même en cas d'erreur
                return "Désolé, j'ai rencontré un problème technique. Pouvez-vous reformuler votre question?";
            }
        }

        private static async Task<string> HandleWebSearch(
            string userMessage,
            GroqService groq,
            WebScraperService scraper,
            ILogger logger)
        {
            logger.LogInformation("Recherche web pour: {Message}", userMessage);

            var scraped = await scraper.GetScrapedAnswerAsync(userMessage);

            if (scraped == null || string.IsNullOrWhiteSpace(scraped.Content))
            {
                logger.LogWarning("Aucun contenu scraped trouvé pour: {Message}", userMessage);
                return "Je n'ai pas trouvé d'information pertinente en ligne. Puis-je vous aider autrement?";
            }

            logger.LogInformation("Contenu trouvé: {Url}, {Length} caractères",
                scraped.Url, scraped.Content.Length);

            string contentToAnalyze = scraped.Content;
            // Limiter la taille pour éviter de dépasser les limites de Groq
            if (contentToAnalyze.Length > 10000)
            {
                contentToAnalyze = contentToAnalyze.Substring(0, 10000) + "...";
            }

            var promptWeb = @$"
Voici une information trouvée en ligne en réponse à la question: '{userMessage}'

SOURCE: {scraped.Url}
TITRE: {scraped.Title}

CONTENU:
{contentToAnalyze}

Réponds à la question de manière concise et informative, en te basant uniquement sur les informations ci-dessus.
Si les informations ne permettent pas de répondre à la question, indique-le clairement.
";

            return await groq.GetCompletionAsync(promptWeb);
        }

        private static async Task<string> HandleCodeGeneration(
            string userMessage,
            GroqService groq,
            ILogger logger)
        {
            logger.LogInformation("Génération de code pour: {Message}", userMessage);

            var promptCode = $@"
L'utilisateur demande du code pour: '{userMessage}'

Fournis le code demandé avec des explications claires. Si possible:
1. Explique brièvement la logique globale
2. Commente les parties importantes du code
3. Fournis des instructions d'utilisation si nécessaire

Si la demande n'est pas claire, propose plusieurs solutions possibles.
";
            return await groq.GetCompletionAsync(promptCode);
        }

        private static Task<string> HandleImageGeneration(
            string userMessage,
            ILogger logger)
        {
            logger.LogInformation("Demande de génération d'image: {Message}", userMessage);

            // Cette fonctionnalité n'est pas encore implémentée
            return Task.FromResult($"Tu m'as demandé une image de: \"{userMessage}\". Cette fonctionnalité sera bientôt disponible!");
        }


        private static async Task<string> HandleConversation(
            string userMessage,
            GroqService groq,
            ILogger logger)
        {
            logger.LogInformation("Conversation standard: {Message}", userMessage);

            var promptDefault = $@"
L'utilisateur dit: '{ userMessage}'

Réponds de façon naturelle, utile et concise. Sois cordial mais pas trop familier.
";
            return await groq.GetCompletionAsync(promptDefault);
        }
    }
}