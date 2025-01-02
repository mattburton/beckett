namespace Beckett.Dashboard.Components.Metrics;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder) => builder.MapGet("/components/metrics", MetricsHandler.Get);
}
