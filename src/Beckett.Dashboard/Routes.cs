global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.AspNetCore.Http;
global using RazorBlade.Support;
using Beckett.Dashboard.MessageStore;
using Beckett.Dashboard.Metrics;

namespace Beckett.Dashboard;

public static class Routes
{
    public static string Prefix { get; private set; } = null!;

    public static RouteGroupBuilder MapBeckettDashboard(this IEndpointRouteBuilder builder, string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        if (!prefix.StartsWith('/'))
        {
            prefix = $"/{prefix}";
        }

        Prefix = prefix;

        return builder.MapGroup(prefix)
            .IndexRoute()
            .MessageStoreRoutes(prefix)
            .MetricsRoutes();
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
