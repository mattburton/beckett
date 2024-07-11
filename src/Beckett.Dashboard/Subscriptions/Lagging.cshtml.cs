namespace Beckett.Dashboard.Subscriptions;

public static class LaggingPage
{
    public static RouteGroupBuilder LaggingPageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/subscriptions/lagging", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Subscriptions.GetLaggingSubscriptions(cancellationToken);

        return new Lagging(new ViewModel(result.Subscriptions));
    }

    public record ViewModel(List<GetLaggingSubscriptionsResult.Subscription> Subscriptions);
}
