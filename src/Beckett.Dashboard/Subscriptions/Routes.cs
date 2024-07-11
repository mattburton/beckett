using Beckett.Dashboard.Subscriptions.Actions;

namespace Beckett.Dashboard.Subscriptions;

public static class Routes
{
    public static DashboardOptions Options { get; private set; } = null!;

    public static RouteGroupBuilder SubscriptionsRoutes(this RouteGroupBuilder builder, DashboardOptions options)
    {
        Options = options;

        return builder
            .IndexPageRoute()
            .LaggingPageRoute()
            .RetriesPageRoute()
            .RetryPageRoute()
            .FailedPageRoute()
            .ActionRoutes();
    }
}
