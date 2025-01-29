namespace Beckett.Dashboard.Subscriptions.Subscription;

public static class SubscriptionEndpoint
{
    public static async Task<IResult> Handle(
        int id,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var result = await dashboard.Subscriptions.GetSubscription(id, cancellationToken);

        return result == null
            ? Results.NotFound()
            : Results.Extensions.Render<Subscription>(new Subscription.ViewModel(result));
    }
}
