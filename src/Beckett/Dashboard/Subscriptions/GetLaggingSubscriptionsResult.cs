namespace Beckett.Dashboard.Subscriptions;

public record GetLaggingSubscriptionsResult(List<GetLaggingSubscriptionsResult.Subscription> Subscriptions)
{
    public record Subscription(string Application, string Name, int TotalLag);
}
