namespace Beckett.Dashboard.Home;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder) =>
        builder.MapGet("/", () => Results.Extensions.Render<Index>());
}
