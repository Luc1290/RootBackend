using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RootBackend.Services;

namespace RootBackend.Core.IntentHandlers
{
    public class ConversationIntentHandler : IIntentHandler
    {
        private readonly GroqService _groq;
        private readonly PromptService _promptService;

        public string IntentName => "discussion";

        public ConversationIntentHandler(
            GroqService groq,
            PromptService promptService)
        {
            _groq = groq;
            _promptService = promptService;
        }

        public async Task<string> HandleAsync(string userMessage, ILogger logger)
        {
            logger.LogInformation("Conversation standard: {Message}", userMessage);

            var prompt = _promptService.GetConversationPrompt(userMessage);
            return await _groq.GetCompletionAsync(prompt);
        }
    }
}