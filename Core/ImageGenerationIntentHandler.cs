using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RootBackend.Core.IntentHandlers
{
    public class ImageGenerationIntentHandler : IIntentHandler
    {
        public string IntentName => "generation_image";

        public Task<string> HandleAsync(string userMessage, ILogger logger)
        {
            logger.LogInformation("Demande de génération d'image: {Message}", userMessage);

            // Cette fonctionnalité n'est pas encore implémentée
            return Task.FromResult($"Tu m'as demandé une image de: \"{userMessage}\". Cette fonctionnalité sera bientôt disponible!");
        }
    }
}