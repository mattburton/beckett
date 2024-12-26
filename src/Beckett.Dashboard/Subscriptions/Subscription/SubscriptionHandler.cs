namespace Beckett.Dashboard.Subscriptions.Subscription;

public static class SubscriptionHandler
{
    public static async Task<IResult> Get(
        string groupName,
        string name,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var result = await dashboard.Subscriptions.GetSubscription(groupName, name, cancellationToken);

        return result == null
            ? Results.NotFound()
            : Results.Extensions.Render<Subscription>(new Subscription.ViewModel(result));
    }
}
