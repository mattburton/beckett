using System.Collections.Concurrent;

namespace Beckett.Subscriptions;

public static class SubscriptionRegistry
{
    private static readonly ConcurrentDictionary<string, Subscription> Subscriptions = new();
    private static readonly Lazy<HashSet<string>> SubscriptionKeys = new(() => Subscriptions.Values.Select(x => x.Id.ToString()).ToHashSet());

    public static bool TryAdd(string name, out Subscription subscription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        subscription = new Subscription(name);

        return Subscriptions.TryAdd(name, subscription);
    }

    public static IEnumerable<Subscription> All() => Subscriptions.Values;

    public static bool HasSubscription(string key) => SubscriptionKeys.Value.Contains(key);

    public static Subscription? GetSubscription(int subscriptionId) =>
        Subscriptions.Values.FirstOrDefault(x => x.Id == subscriptionId);
}
