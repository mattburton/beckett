namespace Beckett.Subscriptions;

public readonly record struct SubscriptionStream(string SubscriptionName, string StreamName)
{
    public int ToAdvisoryLockId() => $"{SubscriptionName}:{StreamName}".GetDeterministicHashCode();
}
