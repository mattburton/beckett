using System.Collections.Concurrent;

namespace Beckett.Subscriptions;

public class SubscriptionRegistry
{
    private static readonly ConcurrentDictionary<string, Subscription> Subscriptions = new();

    public static bool TryAdd(string name, out Subscription subscription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        subscription = new Subscription(name);

        return Subscriptions.TryAdd(name, subscription);
    }

    public static IEnumerable<Subscription> All() => Subscriptions.Values;

    public static Subscription? GetSubscription(string name) => Subscriptions.GetValueOrDefault(name);
}
