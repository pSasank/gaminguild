namespace NeneCM.Core.Events;

/// <summary>
/// A simple publish/subscribe event bus for decoupling game systems.
/// Thread-safe implementation.
/// </summary>
public class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly object _lock = new();

    /// <summary>
    /// Subscribe to events of a specific type.
    /// </summary>
    /// <typeparam name="T">The event type to subscribe to.</typeparam>
    /// <param name="handler">The handler to call when the event is published.</param>
    public void Subscribe<T>(Action<T> handler) where T : GameEvent
    {
        lock (_lock)
        {
            var eventType = typeof(T);
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<Delegate>();
            }
            _subscribers[eventType].Add(handler);
        }
    }

    /// <summary>
    /// Unsubscribe from events of a specific type.
    /// </summary>
    /// <typeparam name="T">The event type to unsubscribe from.</typeparam>
    /// <param name="handler">The handler to remove.</param>
    /// <returns>True if the handler was found and removed.</returns>
    public bool Unsubscribe<T>(Action<T> handler) where T : GameEvent
    {
        lock (_lock)
        {
            var eventType = typeof(T);
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                return handlers.Remove(handler);
            }
            return false;
        }
    }

    /// <summary>
    /// Publish an event to all subscribers.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="gameEvent">The event to publish.</param>
    public void Publish<T>(T gameEvent) where T : GameEvent
    {
        List<Delegate>? handlersCopy;

        lock (_lock)
        {
            var eventType = typeof(T);
            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                return; // No subscribers, that's fine
            }
            // Copy to avoid issues if handlers modify subscriptions
            handlersCopy = new List<Delegate>(handlers);
        }

        foreach (var handler in handlersCopy)
        {
            try
            {
                ((Action<T>)handler)(gameEvent);
            }
            catch (Exception ex)
            {
                // Log but don't crash - one bad handler shouldn't break others
                Console.Error.WriteLine($"EventBus handler error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Gets the number of subscribers for a specific event type.
    /// </summary>
    public int GetSubscriberCount<T>() where T : GameEvent
    {
        lock (_lock)
        {
            var eventType = typeof(T);
            return _subscribers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
        }
    }

    /// <summary>
    /// Clears all subscribers. Useful for testing.
    /// </summary>
    public void ClearAll()
    {
        lock (_lock)
        {
            _subscribers.Clear();
        }
    }

    /// <summary>
    /// Clears subscribers for a specific event type.
    /// </summary>
    public void Clear<T>() where T : GameEvent
    {
        lock (_lock)
        {
            _subscribers.Remove(typeof(T));
        }
    }
}
