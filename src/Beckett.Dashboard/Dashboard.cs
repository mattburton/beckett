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
    Recurring,
    Scheduled,
    Subscriptions
}

public class Component<TModel> : ComponentBase
{
    [Parameter, EditorRequired]
    public required TModel Model { get; set; }
}

public static class ResultExtensions
{
    private const string ModelPropertyName = "Model";

    public static IResult Render<T>(this IResultExtensions _) where T : ComponentBase => new RazorComponentResult<T>();

    public static IResult Render<T>(this IResultExtensions _, object model) where T : ComponentBase =>
        new RazorComponentResult<T>(new Dictionary<string, object?> { { ModelPropertyName, model } });
}

public interface IConfigureRoutes
{
    void Configure(IEndpointRouteBuilder builder);
}

public interface IPagedViewModel
{
    string? Query { get; }
    int Page { get; }
    int PageSize { get; }
    int TotalResults { get; }
    string UrlTemplate { get; }

    int From => TotalResults == 0 ? 0 : Page == 1 ? 1 : (Page - 1) * PageSize + 1;
    int To => From + PageSize > TotalResults ? TotalResults : From - 1 + PageSize;
    int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);
}

public static class PagedViewModelExtensions
{
    public static string CurrentLink(this IPagedViewModel model) => string.Format(
        model.UrlTemplate,
        model.Page,
        model.PageSize,
        model.Query
    );

    public static string FirstPageLink(this IPagedViewModel model) => model.Page > 1
        ? string.Format(model.UrlTemplate, 1, model.PageSize, model.Query)
        : "#";

    public static string PreviousPageLink(this IPagedViewModel model) => model.Page > 1
        ? string.Format(model.UrlTemplate, model.Page - 1, model.PageSize, model.Query)
        : "#";

    public static string NextPageLink(this IPagedViewModel model) => model.Page < model.TotalPages
        ? string.Format(model.UrlTemplate, model.Page + 1, model.PageSize, model.Query)
        : "#";

    public static string LastPageLink(this IPagedViewModel model) => model.Page < model.TotalPages
        ? string.Format(model.UrlTemplate, model.TotalPages, model.PageSize, model.Query)
        : "#";
}

public static class Pagination
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 100;

    public static int ToPageParameter(this int? page) => page is null or < 1 ? DefaultPage : page.Value;

    public static int ToPageSizeParameter(this int? pageSize) =>
        pageSize is null or < 1 ? DefaultPageSize : pageSize.Value;

    public static int ToOffset(int page, int pageSize) => (page - 1) * pageSize;
}
