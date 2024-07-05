namespace Beckett.Dashboard.Subscriptions;

public static class IndexPage
{
    public static RouteGroupBuilder IndexPageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/subscriptions", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Subscriptions.GetSubscriptions(cancellationToken);

        return new Index(new ViewModel(result.Subscriptions));
    }

    public record ViewModel(List<GetSubscriptionsResult.Subscription> Subscriptions);
}
