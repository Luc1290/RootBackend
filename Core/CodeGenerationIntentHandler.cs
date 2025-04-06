using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RootBackend.Services;

namespace RootBackend.Core.IntentHandlers
{
    public class CodeGenerationIntentHandler : IIntentHandler
    {
        private readonly GroqService _groq;
        private readonly PromptService _promptService;

        public string IntentName => "generation_code";

        public CodeGenerationIntentHandler(
            GroqService groq,
            PromptService promptService)
        {
            _groq = groq;
            _promptService = promptService;
        }

        public async Task<string> HandleAsync(string userMessage, ILogger logger)
        {
            logger.LogInformation("Génération de code pour: {Message}", userMessage);

            var prompt = _promptService.GetCodeGenerationPrompt(userMessage);
            return await _groq.GetCompletionAsync(prompt);
        }
    }
}