namespace Beckett.Dashboard.Subscriptions.Actions;

public static class Routes
{
    public static RouteGroupBuilder ActionRoutes(this RouteGroupBuilder builder)
    {
        return builder
            .BulkRetryRoute()
            .RetryRoute()
            .PauseRoute()
            .ResumeRoute()
            .SkipRoute();
    }
}
