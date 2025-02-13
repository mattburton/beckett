namespace Beckett.Dashboard.Metrics;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder) => builder.MapGet("/metrics", MetricsEndpoint.Handle);
}
