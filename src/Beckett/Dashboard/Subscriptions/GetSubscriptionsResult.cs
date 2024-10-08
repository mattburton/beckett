using Beckett.Subscriptions;

namespace Beckett.Dashboard.Subscriptions;

public record GetSubscriptionsResult(List<GetSubscriptionsResult.Subscription> Subscriptions)
{
    public record Subscription(string GroupName, string Name, SubscriptionStatus Status);
}
