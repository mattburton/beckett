namespace Beckett.Dashboard.Subscriptions;

public record GetLaggingSubscriptionsResult(
    List<GetLaggingSubscriptionsResult.Subscription> Subscriptions,
    int TotalResults
)
{
    public record Subscription(string GroupName, string Name, int TotalLag);
}
