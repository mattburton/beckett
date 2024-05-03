namespace Beckett.Subscriptions;

public interface ISubscriptionRegistry
{
    void AddSubscription<THandler, TEvent>(
        string name,
        Func<THandler, TEvent, CancellationToken, Task> handler,
        Action<Subscription>? configure = null
    );

    IEnumerable<(string Name, string[] EventTypes, StartingPosition StartingPosition)> All();
    Type GetType(string name);
    Subscription GetSubscription(string name);
}
