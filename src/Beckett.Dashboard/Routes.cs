global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.AspNetCore.Http;
global using RazorBlade.Support;
using Beckett.Dashboard.MessageStore;
using Beckett.Dashboard.Metrics;
using Beckett.Dashboard.Subscriptions;

namespace Beckett.Dashboard;

public static class Routes
{
    public static string Prefix { get; private set; } = null!;
    public static DashboardOptions Options { get; private set; } = null!;

    public static RouteGroupBuilder MapBeckettDashboard(this IEndpointRouteBuilder builder, string prefix, Action<DashboardOptions>? configure = null)
    {
        Options = new DashboardOptions();

        configure?.Invoke(Options);

        ArgumentNullException.ThrowIfNull(prefix);

        if (!prefix.StartsWith('/'))
        {
            prefix = $"/{prefix}";
        }

        Prefix = prefix;

        var routeGroupBuilder = builder.MapGroup(Prefix)
            .IndexRoute()
            .MetricsRoutes()
            .SubscriptionsRoutes(Prefix, Options);

        if (Options.MessageStoreEnabled)
        {
            routeGroupBuilder.MessageStoreRoutes(Prefix, Options);
        }

        return routeGroupBuilder;
    }
}

[method: TemplateConstructor]
public abstract class HtmlTemplate<T>(T model) : RazorBlade.HtmlTemplate<T>(model), IResult
{
    public Task ExecuteAsync(HttpContext httpContext) => this.RenderAsync(httpContext);
}

public abstract class HtmlTemplate : RazorBlade.HtmlTemplate, IResult
{
    Task IResult.ExecuteAsync(HttpContext httpContext) => this.RenderAsync(httpContext);
}

public abstract class HtmlLayout : RazorBlade.HtmlLayout;

public static class HtmlTemplateExtensions
{
    public static async Task RenderAsync(this RazorBlade.HtmlTemplate template, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.StatusCode = StatusCodes.Status200OK;

        const string contentType = "text/html; charset=utf-8";

        httpContext.Response.ContentType = contentType;

        await using var streamWriter = new StreamWriter(httpContext.Response.Body);

        await template.RenderAsync(streamWriter, httpContext.RequestAborted);

        await streamWriter.FlushAsync(httpContext.RequestAborted);
    }
}
