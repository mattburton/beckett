namespace Beckett.Dashboard.MessageStore;

public static class Routes
{
    public static string Prefix { get; private set; } = null!;
    public static DashboardOptions Options { get; private set; } = null!;

    public static RouteGroupBuilder MessageStoreRoutes(this RouteGroupBuilder builder, string prefix, DashboardOptions options)
    {
        Prefix = prefix;
        Options = options;

        return builder
            .IndexRoute()
            .CorrelatedByRoute()
            .MessageRoute()
            .MessagesRoute()
            .StreamsRoute();
    }
}
