using RootBackend.Core.IntentHandlers;

public class IntentHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _intentHandlers;

    public IntentHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _intentHandlers = new Dictionary<string, Type>();

        // Trouver tous les handlers d'intention dans l'assembly
        var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IIntentHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        // Enregistrez les types dans le dictionnaire en utilisant un attribut ou une convention de nommage
        foreach (var handlerType in handlerTypes)
        {
            // Option 1: Utiliser une convention de nommage (par exemple, "CodeGenerationIntentHandler" -> "code-generation")
            string intentName = handlerType.Name.Replace("IntentHandler", "").ToLower();
            _intentHandlers[intentName] = handlerType;

            // Assurez-vous que ces types sont enregistrés dans le conteneur DI
            // Ceci devrait être fait dans Startup.cs ou Program.cs
        }

        // Ou, récupérez tous les handlers déjà instanciés et enregistrez-les par leur nom d'intention
        var handlers = handlerTypes
            .Select(t => serviceProvider.GetService(t) as IIntentHandler)
            .Where(h => h != null);

        foreach (var handler in handlers)
        {
            if (handler != null)
            {
                _intentHandlers[handler.IntentName] = handler.GetType();
            }
        }
    }

    public IIntentHandler GetHandler(string intent)
    {
        // Le reste du code reste inchangé
        if (_intentHandlers.TryGetValue(intent, out var handlerType))
        {
            // Utiliser le ServiceProvider pour obtenir l'instance correcte
            return (IIntentHandler)_serviceProvider.GetRequiredService(handlerType);
        }

        return _serviceProvider.GetRequiredService<ConversationIntentHandler>();
    }
}