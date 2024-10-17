using Beckett.Subscriptions;

namespace Beckett.Dashboard.Subscriptions;

public record GetSubscriptionsResult(List<GetSubscriptionsResult.Subscription> Subscriptions, int TotalResults)
{
    public record Subscription(string GroupName, string Name, SubscriptionStatus Status);
}
