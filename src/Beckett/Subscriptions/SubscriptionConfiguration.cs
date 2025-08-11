namespace Beckett.Subscriptions;

public record SubscriptionConfiguration(
    long SubscriptionId,
    string GroupName,
    string SubscriptionName,
    string? Category,
    string? StreamName,
    string[] MessageTypes,
    int Priority,
    bool SkipDuringReplay
);
