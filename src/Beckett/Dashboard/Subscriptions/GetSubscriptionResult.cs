using Beckett.Subscriptions;

namespace Beckett.Dashboard.Subscriptions;

public record GetSubscriptionResult(string GroupName, string Name, SubscriptionStatus Status);
