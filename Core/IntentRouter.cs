using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RootBackend.Core.IntentHandlers;
using RootBackend.Models;

namespace RootBackend.Core
{
    public class IntentRouter
    {
        private readonly IntentHandlerFactory _handlerFactory;

        public IntentRouter(IntentHandlerFactory handlerFactory)
        {
            _handlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
        }

        public async Task<string> HandleAsync(
             NlpResponse nlp,
             string userMessage,
             ILogger logger)
        {
            if (nlp == null)
            {
                logger.LogWarning("NLP response null, fallback à l'intention 'discussion'");
                nlp = new NlpResponse { Intent = "discussion" };
            }

            logger.LogInformation("Traitement de l'intention: {Intent} (confiance: {Confidence}) pour le message: {Message}",
                nlp.Intent, nlp.Confidence, userMessage);

            // Si c'est une question factuelle et que la confiance pour "discussion" est faible,
            // essayons plutôt la recherche web
            if (nlp.Intent == "discussion" && nlp.Confidence < 0.7 &&
                (userMessage.Contains("?") ||
                 userMessage.StartsWith("qui", StringComparison.OrdinalIgnoreCase) ||
                 userMessage.StartsWith("que", StringComparison.OrdinalIgnoreCase) ||
                 userMessage.StartsWith("qu'", StringComparison.OrdinalIgnoreCase) ||
                 userMessage.StartsWith("où", StringComparison.OrdinalIgnoreCase) ||
                 userMessage.StartsWith("quand", StringComparison.OrdinalIgnoreCase) ||
                 userMessage.StartsWith("comment", StringComparison.OrdinalIgnoreCase) ||
                 userMessage.StartsWith("pourquoi", StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogInformation("Question factuelle détectée, redirection vers recherche_web");
                var webSearchHandler = _handlerFactory.GetHandler("recherche_web");
                return await webSearchHandler.HandleAsync(userMessage, logger);
            }

            try
            {
                var handler = _handlerFactory.GetHandler(nlp.Intent);
                return await handler.HandleAsync(userMessage, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur lors du traitement de l'intention: {Intent}", nlp.Intent);
                return "Une erreur est survenue lors du traitement de votre demande.";
            }
        }
    }

}
