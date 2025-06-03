namespace Beckett.Subscriptions;

public interface ISubscriptionContext
{
    string GroupName { get; }
    string Name { get; }
    SubscriptionStatus Status { get; }
    bool IsReplay { get; }
}

public record SubscriptionContext(string GroupName, string Name, SubscriptionStatus Status) : ISubscriptionContext
{
    public bool IsReplay => Status == SubscriptionStatus.Replay;
}
