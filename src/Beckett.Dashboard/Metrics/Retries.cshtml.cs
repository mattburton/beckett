namespace Beckett.Dashboard.Metrics;

public static class RetriesComponent
{
    public static RouteGroupBuilder RetriesRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/metrics/retries", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Metrics.GetSubscriptionRetryCount(cancellationToken);

        return new Retries(new ViewModel(result));
    }

    public record ViewModel(long Value);
}
