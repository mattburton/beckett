namespace TaskHub.Infrastructure.Routing;

public static class RouteGroupBuilderExtensions
{
    public static RouteGroupBuilder MapRoutes(this RouteGroupBuilder builder)
    {
        var routes = typeof(IConfigureRoutes).Assembly
            .GetTypes()
            .Where(x => x.IsAssignableTo(typeof(IConfigureRoutes)) && x is { IsAbstract: false, IsInterface: false })
            .Select(Activator.CreateInstance)
            .Cast<IConfigureRoutes>();

        foreach (var routeConfiguration in routes)
        {
            routeConfiguration.Routes(builder);
        }

        return builder;
    }
}
