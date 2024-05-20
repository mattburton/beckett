namespace Beckett.Subscriptions;

public class SubscriptionRegistry : ISubscriptionRegistry
{
    private readonly Dictionary<string, Subscription> _subscriptions = new();

    public bool TryAdd(string name, out Subscription subscription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_subscriptions.ContainsKey(name))
        {
            subscription = null!;
            return false;
        }

        subscription = new Subscription(name);

        _subscriptions.Add(name, subscription);

        return true;
    }

    public IEnumerable<Subscription> All() => _subscriptions.Values;

    public Subscription? GetSubscription(string name) => _subscriptions.GetValueOrDefault(name);
}
