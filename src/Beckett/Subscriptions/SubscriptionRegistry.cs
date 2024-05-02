using Beckett.Events;

namespace Beckett.Subscriptions;

internal static class SubscriptionRegistry
{
    private static readonly Dictionary<string, Subscription> Subscriptions = new();
    private static readonly Dictionary<string, Type> NameTypeMap = new();

    public static void AddSubscription<THandler, TEvent>(
        string name,
        Func<THandler, TEvent, CancellationToken, Task> handler,
        Action<Subscription>? configure = null
    )
    {
        var handlerType = typeof(THandler);

        if (!NameTypeMap.TryAdd(name, handlerType))
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

        Subscriptions.Add(name, configuration);
    }

    public static IEnumerable<(string Name, string[] EventTypes, StartingPosition StartingPosition)> All()
    {
        foreach (var subscription in Subscriptions.Values)
        {
            var eventTypes = subscription.EventTypes.Select(EventTypeMap.GetName).ToArray();

            yield return (subscription.Name, eventTypes, subscription.StartingPosition);
        }
    }

    public static Type GetType(string name)
    {
        if (!Subscriptions.TryGetValue(name, out var subscription))
        {
            throw new InvalidOperationException($"Unknown subscription: {name}");
        }

        return subscription.Type;
    }

    public static Subscription GetSubscription(string name)
    {
        if (!Subscriptions.TryGetValue(name, out var subscription))
        {
            throw new InvalidOperationException($"Unknown subscription: {name}");
        }

        return subscription;
    }
}

