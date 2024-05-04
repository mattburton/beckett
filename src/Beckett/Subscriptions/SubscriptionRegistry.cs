using Beckett.Events;

namespace Beckett.Subscriptions;

public class SubscriptionRegistry(IEventTypeMap eventTypeMap) : ISubscriptionRegistry
{
    private readonly Dictionary<string, Subscription> _subscriptions = new();
    private readonly Dictionary<string, Type> _nameTypeMap = new();

    public void AddSubscription<THandler, TEvent>(
        string name,
        SubscriptionHandler<THandler, TEvent> handler,
        Action<Subscription>? configure = null
    )
    {
        var handlerType = typeof(THandler);

        if (!_nameTypeMap.TryAdd(name, handlerType))
        {
            throw new Exception($"There is already a subscription with the name {name}");
        }

        var configuration = new Subscription
        {
            Name = name,
            Type = handlerType,
            StartingPosition = StartingPosition.Earliest
        };

        configure?.Invoke(configuration);

        configuration.SubscribeTo<TEvent>();

        configuration.Handler = (h, e, t) => handler((THandler)h, (TEvent)e, t);

        _subscriptions.Add(name, configuration);
    }

    public IEnumerable<(string Name, string[] EventTypes, StartingPosition StartingPosition)> All()
    {
        foreach (var subscription in _subscriptions.Values)
        {
            var eventTypes = subscription.EventTypes.Select(eventTypeMap.GetName).ToArray();

            yield return (subscription.Name, eventTypes, subscription.StartingPosition);
        }
    }

    public Type GetType(string name)
    {
        if (!_subscriptions.TryGetValue(name, out var subscription))
        {
            throw new InvalidOperationException($"Unknown subscription: {name}");
        }

        return subscription.Type;
    }

    public Subscription? GetSubscription(string name)
    {
        return _subscriptions.GetValueOrDefault(name);
    }
}

