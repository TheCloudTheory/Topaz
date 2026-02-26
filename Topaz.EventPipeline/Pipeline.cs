using Topaz.Shared;

namespace Topaz.EventPipeline;

public sealed class Pipeline(ITopazLogger logger)
{
    private static readonly Dictionary<string, List<Action<object?>>> Handlers = new();
    
    public void RegisterHandler<TData>(string eventName, Action<TData?> handler) where TData : class
    {
        logger.LogDebug(nameof(Pipeline), nameof(RegisterHandler), "Registering handler for event `{0}`.", eventName);

        Action<object?> genericHandler = obj =>
        {
            switch (obj)
            {
                case null:
                    handler(null);
                    return;
                case TData typed:
                    handler(typed);
                    return;
                default:
                    throw new ArgumentException(
                        $"Handler for event `{eventName}` expected `{typeof(TData).FullName}`, but got `{obj.GetType().FullName}`.");
            }
        };
        
        if (!Handlers.TryGetValue(eventName, out var handlers))
        {
            Handlers[eventName] = [genericHandler];
            return;
        }
        
        handlers.Add(genericHandler);
    }

    public void TriggerEvent<T, TEventDefinition>(TEventDefinition @event) where TEventDefinition : IEventDefinition<T>
    {
        logger.LogDebug(nameof(Pipeline), nameof(TriggerEvent), "Triggering event `{0}` with data `{1}`.", @event.Name, @event.Data);
        
        if (!Handlers.TryGetValue(@event.Name, out var handlers))
        {
            logger.LogWarning($"No handlers registered for event `{@event.Name}`");
            return;
        }
        
        foreach (var handler in handlers)
        {
            handler(@event.Data);
        }
    }
}