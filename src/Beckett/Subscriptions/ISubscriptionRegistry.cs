namespace Beckett.Subscriptions;

public interface ISubscriptionRegistry
{
    void AddSubscription<THandler, TEvent>(
        string name,
        SubscriptionHandler<THandler, TEvent> handler,
        Action<Subscription>? configure = null
    );

    IEnumerable<(string Name, string[] EventTypes, StartingPosition StartingPosition)> All();
    Type GetType(string name);
    Subscription GetSubscription(string name);
}

public delegate Task SubscriptionHandler<in THandler, in TEvent>(
    THandler handler,
    TEvent @event,
    CancellationToken cancellationToken
);
