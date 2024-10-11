namespace Beckett.Dashboard.Subscriptions;

public static class SubscriptionPage
{
    public static RouteGroupBuilder SubscriptionPageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/subscriptions/{groupName}/{name}", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(string groupName, string name, IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Subscriptions.GetSubscription(groupName, name, cancellationToken);

        return result == null ? Results.NotFound() : new Subscription(new ViewModel(result));
    }

    public record ViewModel(GetSubscriptionResult Details);
}
