using Beckett.Dashboard.Subscriptions.Actions;

namespace Beckett.Dashboard.Subscriptions;

public static class Routes
{
    public static string Prefix { get; private set; } = null!;
    public static DashboardOptions Options { get; private set; } = null!;

    public static RouteGroupBuilder SubscriptionRoutes(this RouteGroupBuilder builder, string prefix, DashboardOptions options)
    {
        Prefix = prefix;
        Options = options;

        return builder
            .ActionRoutes()
            .CheckpointPageRoute()
            .FailedPageRoute()
            .IndexPageRoute()
            .LaggingPageRoute()
            .ReservationsPageRoute()
            .RetriesPageRoute()
            .SubscriptionPageRoute();
    }
}
