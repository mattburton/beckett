namespace Beckett.Subscriptions;

public interface ISubscriptionRegistry
{
    void AddSubscription<THandler, TEvent>(
        string name,
        SubscriptionHandler<THandler, TEvent> handler,
        Action<Subscription>? configure = null
    );

    void AddSubscription<THandler, TEvent>(
        string name,
        SubscriptionHandlerWithContext<THandler, TEvent> handler,
        Action<Subscription>? configure = null
    );

    void AddSubscription<THandler>(
        string name,
        SubscriptionHandler<THandler> handler,
        Action<Subscription>? configure = null
    );

    IEnumerable<(string Name, string[] EventTypes, StartingPosition StartingPosition)> All();
    Type GetType(string name);
    Subscription? GetSubscription(string name);
}

public delegate Task SubscriptionHandler<in THandler, in TEvent>(
    THandler handler,
    TEvent @event,
    CancellationToken cancellationToken
);

public delegate Task SubscriptionHandlerWithContext<in THandler, in TEvent>(
    THandler handler,
    TEvent @event,
    IEventContext context,
    CancellationToken cancellationToken
);

public delegate Task SubscriptionHandler<in THandler>(
    THandler handler,
    IEventContext context,
    CancellationToken cancellationToken
);
