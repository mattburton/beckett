using Beckett.Messages;

namespace Beckett.Subscriptions;

public class SubscriptionRegistry(IMessageTypeMap messageTypeMap) : ISubscriptionRegistry
{
    private readonly Dictionary<string, Subscription> _subscriptions = new();
    private readonly Dictionary<string, Type> _nameTypeMap = new();

    public void AddSubscription<THandler, TMessage>(
        string name,
        SubscriptionHandler<THandler, TMessage> handler,
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
            StartingPosition = StartingPosition.Latest
        };

        configure?.Invoke(configuration);

        configuration.SubscribeTo<TMessage>();

        configuration.Handler = (h, m, t) => handler((THandler)h, (TMessage)m, t);

        _subscriptions.Add(name, configuration);
    }

    public void AddSubscription<THandler, TMessage>(
        string name,
        SubscriptionHandlerWithContext<THandler, TMessage> handler,
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
            StartingPosition = StartingPosition.Latest
        };

        configure?.Invoke(configuration);

        configuration.SubscribeTo<TMessage>();

        configuration.Handler = (h, c, t) => handler((THandler)h, (TMessage)((IMessageContext)c).Message, (IMessageContext)c, t);

        configuration.MessageContextHandler = true;

        _subscriptions.Add(name, configuration);
    }

    public void AddSubscription<THandler>(string name, SubscriptionHandler<THandler> handler, Action<Subscription>? configure = null)
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
            StartingPosition = StartingPosition.Latest
        };

        configure?.Invoke(configuration);

        configuration.Handler = (h, c, t) => handler((THandler)h, (IMessageContext)c, t);

        configuration.MessageContextHandler = true;

        if (configuration.MessageTypes.Count == 0)
        {
            throw new Exception(
                $"Subscription configuration error for {name} - subscriptions that handle {nameof(IMessageContext)} instead of a specific message type must subscribe to one or more message types explicitly by calling configuration.SubscribeTo<MessageType>()"
            );
        }

        _subscriptions.Add(name, configuration);
    }

    public IEnumerable<(string Name, string[] MessageTypes, StartingPosition StartingPosition)> All()
    {
        foreach (var subscription in _subscriptions.Values)
        {
            var messageTypes = subscription.MessageTypes.Select(messageTypeMap.GetName).ToArray();

            yield return (subscription.Name, messageTypes, subscription.StartingPosition);
        }
    }

    public Type GetType(string name)
    {
        if (!_subscriptions.TryGetValue(name, out var subscription))
        {
            throw new Exception($"Unknown subscription: {name}");
        }

        return subscription.Type;
    }

    public Subscription? GetSubscription(string name)
    {
        return _subscriptions.GetValueOrDefault(name);
    }
}

