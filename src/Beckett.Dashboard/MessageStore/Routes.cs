namespace Beckett.Dashboard.MessageStore;

public static class Routes
{
    public static DashboardOptions Options { get; private set; } = null!;

    public static RouteGroupBuilder MessageStoreRoutes(this RouteGroupBuilder builder, DashboardOptions options)
    {
        Options = options;

        return builder
            .IndexRoute()
            .StreamsRoute()
            .MessageRoute()
            .MessagesRoute();
    }
}
