namespace Beckett.Subscriptions;

public interface ISubscriptionRegistry
{
    void AddSubscription<THandler, TMessage>(
        string name,
        SubscriptionHandler<THandler, TMessage> handler,
        Action<Subscription>? configure = null
    );

    void AddSubscription<THandler, TMessage>(
        string name,
        SubscriptionHandlerWithContext<THandler, TMessage> handler,
        Action<Subscription>? configure = null
    );

    void AddSubscription<THandler>(
        string name,
        SubscriptionHandler<THandler> handler,
        Action<Subscription>? configure = null
    );

    void AddSubscription(
        string name,
        SubscriptionHandler handler,
        Action<Subscription>? configure = null
    );

    IEnumerable<Subscription> All();
    Type GetType(string name);
    Subscription? GetSubscription(string name);
}

public delegate Task SubscriptionHandler<in THandler, in TMessage>(
    THandler handler,
    TMessage message,
    CancellationToken cancellationToken
);

public delegate Task SubscriptionHandlerWithContext<in THandler, in TMessage>(
    THandler handler,
    TMessage message,
    IMessageContext context,
    CancellationToken cancellationToken
);

public delegate Task SubscriptionHandler<in THandler>(
    THandler handler,
    IMessageContext context,
    CancellationToken cancellationToken
);

public delegate Task SubscriptionHandler(
    IMessageContext context,
    CancellationToken cancellationToken
);
