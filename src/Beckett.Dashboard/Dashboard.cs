global using System.Web;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Http.HttpResults;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.Extensions.Primitives;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Dashboard;

public static class Dashboard
{
    internal static readonly DashboardOptions Options = new();
    internal static string Prefix = "/beckett";

    public static void AddBeckettDashboard(this IServiceCollection services)
    {
        if (services.Any(x => x.ServiceType == typeof(IComponentPrerenderer)))
        {
            return;
        }

        services.AddRazorComponents();
    }

    public static RouteGroupBuilder MapBeckettDashboard(
        this IEndpointRouteBuilder builder,
        string prefix,
        Action<DashboardOptions>? configure = null
    )
    {
        configure?.Invoke(Options);

        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        if (!prefix.StartsWith('/'))
        {
            prefix = $"/{prefix}";
        }

        Prefix = prefix;

        var routes = builder.MapGroup(Prefix);

        RegisterRoutes(routes);

        return routes;
    }

    private static void RegisterRoutes(IEndpointRouteBuilder builder)
    {
        var routeConfigurations = typeof(Dashboard).Assembly
            .GetTypes()
            .Where(x => x.IsAssignableTo(typeof(IConfigureRoutes)) && x is { IsAbstract: false, IsInterface: false })
            .Select(Activator.CreateInstance)
            .Cast<IConfigureRoutes>();

        foreach (var routeConfiguration in routeConfigurations)
        {
            routeConfiguration.Configure(builder);
        }
    }
}

public enum Area
{
    Home,
    MessageStore,
    Subscriptions
}

public class Component<TModel> : ComponentBase
{
    [Parameter, EditorRequired] public required TModel Model { get; set; }
}

public static class ResultExtensions
{
    private const string ModelPropertyName = "Model";

    public static IResult Render<T>(this IResultExtensions _) where T : ComponentBase
    {
        return new RazorComponentResult<T>();
    }

    public static IResult Render<T>(this IResultExtensions _, object model) where T : ComponentBase
    {
        return new RazorComponentResult<T>(new Dictionary<string, object?> { { ModelPropertyName, model } });
    }
}

public interface IConfigureRoutes
{
    void Configure(IEndpointRouteBuilder builder);
}
