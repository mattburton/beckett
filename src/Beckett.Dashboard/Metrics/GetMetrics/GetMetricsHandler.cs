namespace Beckett.Dashboard.Metrics.GetMetrics;

public static class GetMetricsHandler
{
    public static async Task<IResult> Get(IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Metrics.GetSubscriptionMetrics(cancellationToken);

        return Results.Extensions.Render<Metrics>(
            new Metrics.ViewModel(result.Lagging, result.Retries, result.Failed, false)
        );
    }
}
