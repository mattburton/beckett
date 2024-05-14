namespace Beckett.Subscriptions;

public interface ISubscriptionRegistry
{
    bool TryAdd(string name, out Subscription subscription);
    IEnumerable<Subscription> All();
    Subscription? GetSubscription(string name);
}

public delegate Task TypedMessageHandler<in THandler, in TMessage>(
    THandler handler,
    TMessage message,
    CancellationToken cancellationToken
);

public delegate Task TypedMessageAndContextHandler<in THandler, in TMessage>(
    THandler handler,
    TMessage message,
    IMessageContext context,
    CancellationToken cancellationToken
);

public delegate Task MessageContextHandler<in THandler>(
    THandler handler,
    IMessageContext context,
    CancellationToken cancellationToken
);

public delegate Task StaticTypedMessageHandler<in TMessage>(
    TMessage message,
    CancellationToken cancellationToken
);

public delegate Task StaticTypedMessageAndContextHandler<in TMessage>(
    TMessage message,
    IMessageContext context,
    CancellationToken cancellationToken
);

public delegate Task StaticMessageContextHandler(
    IMessageContext context,
    CancellationToken cancellationToken
);
