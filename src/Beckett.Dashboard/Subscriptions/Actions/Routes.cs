namespace Beckett.Dashboard.Subscriptions.Actions;

public static class Routes
{
    public static RouteGroupBuilder ActionRoutes(this RouteGroupBuilder builder)
    {
        return builder
            .BulkRetryRoute()
            .BulkSkipRoute()
            .RetryRoute()
            .PauseRoute()
            .ResumeRoute()
            .SkipRoute();
    }
}
