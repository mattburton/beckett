using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Subscription;

public static class SubscriptionEndpoint
{
    public static async Task<IResult> Handle(
        string groupName,
        string name,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        var result = await database.Execute(new SubscriptionQuery(groupName, name), cancellationToken);

        return result == null
            ? Results.NotFound()
            : Results.Extensions.Render<Subscription>(new Subscription.ViewModel(result));
    }
}
