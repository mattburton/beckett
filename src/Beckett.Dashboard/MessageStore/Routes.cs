namespace Beckett.Dashboard.MessageStore;

public static class Routes
{
    public static string Prefix { get; private set; } = null!;

    public static RouteGroupBuilder MessageStoreRoutes(this RouteGroupBuilder builder, string prefix)
    {
        Prefix = prefix;

        return builder
            .IndexRoute()
            .StreamsRoute()
            .MessageRoute()
            .MessagesRoute();
    }
}
