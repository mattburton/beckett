using Beckett.Dashboard.Subscriptions.Actions;

namespace Beckett.Dashboard.Subscriptions;

public static class Routes
{
    public static string Prefix { get; private set; } = null!;

    public static RouteGroupBuilder SubscriptionsRoutes(this RouteGroupBuilder builder, string prefix)
    {
        Prefix = prefix;

        return builder
            .IndexPageRoute()
            .RetriesPageRoute()
            .FailedPageRoute()
            .ActionRoutes();
    }
}
