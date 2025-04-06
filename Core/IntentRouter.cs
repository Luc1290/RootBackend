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
            _handlerFactory = handlerFactory;
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

            logger.LogInformation("Traitement de l'intention: {Intent} pour le message: {Message}",
                nlp.Intent, userMessage);

            try
            {
                var handler = _handlerFactory.GetHandler(nlp.Intent);
                return await handler.HandleAsync(userMessage, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur lors du traitement de l'intention {Intent}: {Message}",
                    nlp.Intent, ex.Message);

                // Toujours retourner une réponse, même en cas d'erreur
                return "Désolé, j'ai rencontré un problème technique. Pouvez-vous reformuler votre question?";
            }
        }
    }
}