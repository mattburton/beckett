namespace Beckett.Dashboard.Messages;

public static class Routes
{
    public static string Prefix { get; private set; } = null!;

    public static RouteGroupBuilder MessageRoutes(this RouteGroupBuilder builder, string prefix)
    {
        Prefix = prefix;

        return builder
            .IndexRoute();
    }
}
