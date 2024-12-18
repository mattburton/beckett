namespace Beckett.Dashboard.Metrics.Handlers;

public static class FailedHandler
{
    public static async Task<IResult> Get(IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Metrics.GetSubscriptionFailedCount(cancellationToken);

        return Results.Extensions.Render<Failed>(new Failed.ViewModel(result));
    }
}
