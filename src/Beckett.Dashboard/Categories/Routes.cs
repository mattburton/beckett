namespace Beckett.Dashboard.Categories;

public static class Routes
{
    public static string Prefix { get; private set; } = null!;

    public static RouteGroupBuilder CategoryRoutes(this RouteGroupBuilder builder, string prefix)
    {
        Prefix = prefix;

        return builder
            .IndexRoute()
            .StreamsRoute()
            .MessagesRoute();
    }
}
