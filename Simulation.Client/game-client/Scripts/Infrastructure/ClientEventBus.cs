using Microsoft.Extensions.Logging;

namespace GameClient.Scripts.Infrastructure;

/// <summary>
/// Simple event bus implementation for the client
/// </summary>
public interface IClientEventBus
{
    void Publish<TEvent>(TEvent eventData) where TEvent : class;
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
}

/// <summary>
/// In-memory event bus implementation
/// </summary>
public class ClientEventBus : IClientEventBus
{
    private readonly Dictionary<Type, List<object>> _handlers = new();
    private readonly ILogger<ClientEventBus> _logger;

    public ClientEventBus(ILogger<ClientEventBus> logger)
    {
        _logger = logger;
    }

    public void Publish<TEvent>(TEvent eventData) where TEvent : class
    {
        var eventType = typeof(TEvent);
        
        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            _logger.LogTrace("No handlers registered for event type {EventType}", eventType.Name);
            return;
        }
        
        foreach (var handler in handlers)
        {
            try
            {
                ((Action<TEvent>)handler)(eventData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing event handler for {EventType}", eventType.Name);
            }
        }
    }

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        var eventType = typeof(TEvent);
        
        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            handlers = new List<object>();
            _handlers[eventType] = handlers;
        }
        
        handlers.Add(handler);
        _logger.LogDebug("Handler subscribed for event type {EventType}", eventType.Name);
    }
    
    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        var eventType = typeof(TEvent);
        
        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            return;
        }
        
        handlers.Remove(handler);
        _logger.LogDebug("Handler unsubscribed from event type {EventType}", eventType.Name);
    }
}