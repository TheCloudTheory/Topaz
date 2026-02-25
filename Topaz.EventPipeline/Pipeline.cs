using Topaz.Shared;

namespace Topaz.EventPipeline;

public sealed class Pipeline(ITopazLogger logger)
{
    private readonly Dictionary<string, List<Action<object?>>> _handlers = new();
    
    public void RegisterHandler<T>(IEventDefinition<T> @event, Action<object?> handler)
    {
        logger.LogDebug(nameof(Pipeline), nameof(RegisterHandler), "Registering handler for event `{0}`.", @event.Name);
        
        if (!_handlers.TryGetValue(@event.Name, out var handlers))
        {
            _handlers[@event.Name] = [handler];
            return;
        }
        
        handlers.Add(handler);
    }

    public void TriggerEvent<T>(IEventDefinition<T> @event)
    {
        logger.LogDebug(nameof(Pipeline), nameof(TriggerEvent), "Triggering event `{0}` with data `{1}`.", @event.Name, @event.Data);
        
        if (!_handlers.TryGetValue(@event.Name, out var handlers))
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