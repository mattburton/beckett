namespace Beckett.Subscriptions;

public interface ISubscriptionRegistry
{
    bool TryAdd(string name, out Subscription subscription);
    IEnumerable<Subscription> All();
    Subscription? GetSubscription(string name);
}
