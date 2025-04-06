using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RootBackend.Core.IntentHandlers
{
    public interface IIntentHandler
    {
        string IntentName { get; }
        Task<string> HandleAsync(string userMessage, ILogger logger);
    }
}