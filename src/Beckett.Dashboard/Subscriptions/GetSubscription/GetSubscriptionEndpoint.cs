namespace Beckett.Dashboard.Subscriptions.GetSubscription;

public static class GetSubscriptionEndpoint
{
    public static async Task<IResult> Handle(
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
