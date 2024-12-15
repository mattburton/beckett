namespace Beckett.Dashboard.Components.Metrics;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/metrics");

        routes.MapGet("/failed", Failed.Get);
        routes.MapGet("/lag", Lag.Get);
        routes.MapGet("/retries", Retries.Get);
    }
}
