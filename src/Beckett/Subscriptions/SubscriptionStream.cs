namespace Beckett.Subscriptions;

internal readonly record struct SubscriptionStream(string SubscriptionName, string StreamName)
{
    public int ToAdvisoryLockId() => $"{SubscriptionName}:{StreamName}".GetDeterministicHashCode();

    public Type SubscriptionType => SubscriptionRegistry.GetType(SubscriptionName);

    public void EnsureSubscriptionTypeIsValid() => SubscriptionRegistry.GetType(SubscriptionName);
}
