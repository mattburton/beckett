using Beckett.Dashboard.Metrics.Handlers;

namespace Beckett.Dashboard.Metrics;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/metrics");

        routes.MapGet("/failed", FailedHandler.Get);
        routes.MapGet("/lag", LagHandler.Get);
        routes.MapGet("/retries", RetriesHandler.Get);
    }
}
