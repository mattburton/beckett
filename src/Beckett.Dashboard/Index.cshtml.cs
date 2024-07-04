namespace Beckett.Dashboard;

public static class IndexPage
{
    public static RouteGroupBuilder IndexRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/", () => new Index());

        return builder;
    }
}
