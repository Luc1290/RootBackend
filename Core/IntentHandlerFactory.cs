using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using RootBackend.Core.IntentHandlers;

namespace RootBackend.Core
{
    public class IntentHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, Type> _intentHandlers;

        public IntentHandlerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            // Trouver tous les handlers d'intention dans l'assembly
            var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IIntentHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            _intentHandlers = new Dictionary<string, Type>();

            foreach (var handlerType in handlerTypes)
            {
                // Créer temporairement une instance pour accéder à la propriété IntentName
                var handler = (IIntentHandler?)Activator.CreateInstance(handlerType);
                if (handler != null)
                {
                    _intentHandlers[handler.IntentName] = handlerType;
                }
            }
        }

        public IIntentHandler GetHandler(string intent)
        {
            if (_intentHandlers.TryGetValue(intent, out var handlerType))
            {
                var handler = _serviceProvider.GetService(handlerType) as IIntentHandler;
                if (handler != null)
                {
                    return handler;
                }
            }

            // Fallback handler pour les intentions inconnues
            return _serviceProvider.GetRequiredService<ConversationIntentHandler>();
        }

    }
}
