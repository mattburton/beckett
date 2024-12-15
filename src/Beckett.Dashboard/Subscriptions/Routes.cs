namespace Beckett.Dashboard.Subscriptions;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/subscriptions");

        routes.MapGet("/", Index.Get);
        routes.MapGet("/checkpoints/{id:long}", Checkpoint.Get);
        routes.MapGet("/failed", Failed.Get);
        routes.MapGet("/lagging", Lagging.Get);
        routes.MapGet("/reservations", Reservations.Get);
        routes.MapGet("/retries", Retries.Get);
        routes.MapGet("/{groupName}/{name}", Subscription.Get);
    }
}
