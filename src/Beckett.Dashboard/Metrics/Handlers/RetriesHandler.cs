namespace Beckett.Dashboard.Metrics.Handlers;

public static class RetriesHandler
{
    public static async Task<IResult> Get(IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Metrics.GetSubscriptionRetryCount(cancellationToken);

        return Results.Extensions.Render<Retries>(new Retries.ViewModel(result));
    }
}
