namespace Beckett.Dashboard.Metrics;

public static class Routes
{
    public static RouteGroupBuilder MetricsRoutes(this RouteGroupBuilder builder)
    {
        return builder
            .LagRoute()
            .RetriesRoute()
            .FailedRoute();
    }
}
