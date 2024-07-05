namespace Beckett.Dashboard.Subscriptions;

public record GetSubscriptionsResult(List<GetSubscriptionsResult.Subscription> Subscriptions)
{
    public record Subscription(string Application, string Name, int TotalLag);
}
