namespace Beckett.Dashboard.Metrics.GetMetrics;

public static class GetMetricsEndpoint
{
    public static async Task<IResult> Handle(IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Metrics.GetSubscriptionMetrics(cancellationToken);

        return Results.Extensions.Render<Metrics>(
            new Metrics.ViewModel(result.Lagging, result.Retries, result.Failed, false)
        );
    }
}
