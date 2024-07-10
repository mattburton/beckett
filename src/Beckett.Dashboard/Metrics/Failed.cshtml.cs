namespace Beckett.Dashboard.Metrics;

public static class FailedComponent
{
    public static RouteGroupBuilder FailedRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/metrics/failed", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Metrics.GetSubscriptionFailedCount(cancellationToken);

        return new Failed(new ViewModel(result));
    }

    public record ViewModel(long Value);
}
