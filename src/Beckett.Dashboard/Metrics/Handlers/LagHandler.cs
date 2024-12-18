namespace Beckett.Dashboard.Metrics.Handlers;

public static class LagHandler
{
    public static async Task<IResult> Get(IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Metrics.GetSubscriptionLag(cancellationToken);

        return Results.Extensions.Render<Lag>(new Lag.ViewModel(result));
    }
}
