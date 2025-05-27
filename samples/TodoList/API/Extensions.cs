namespace API;

public static class Extensions
{
    public static RouteGroupBuilder With(this RouteGroupBuilder builder, Action<RouteGroupBuilder> route)
    {
        route(builder);

        return builder;
    }
}
