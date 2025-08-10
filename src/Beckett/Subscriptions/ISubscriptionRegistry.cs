namespace Beckett.Subscriptions;

public interface ISubscriptionRegistry
{
    Task Initialize(CancellationToken cancellationToken = default);
    long? GetSubscriptionId(string groupName, string subscriptionName);
    (string GroupName, string SubscriptionName)? GetSubscription(long subscriptionId);
}
