using Beckett.Subscriptions;

namespace Beckett.Dashboard.Subscriptions;

public record GetSubscriptionsResult(List<GetSubscriptionsResult.Subscription> Subscriptions)
{
    public record Subscription(string Group, string Name, SubscriptionStatus Status);
}
