namespace Beckett.Dashboard.Components;

public static class Routes
{
    public static RouteGroupBuilder ComponentRoutes(this RouteGroupBuilder builder)
    {
        return builder
            .LagRoute()
            .RetriesRoute()
            .FailedRoute();
    }
}
