namespace Beckett.Dashboard.Metrics.Components;

public static class LagComponent
{
    public static RouteGroupBuilder LagRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/metrics/components/lag", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Metrics.GetSubscriptionLag(cancellationToken);

        return new Lag(new ViewModel(result));
    }

    public record ViewModel(long Value);
}
