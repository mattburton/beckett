using Beckett.Dashboard.Subscriptions.Handlers;

namespace Beckett.Dashboard.Subscriptions;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/subscriptions");

        routes.MapGet("/", IndexHandler.Get);
        routes.MapGet("/checkpoints/{id:long}", CheckpointHandler.Get);
        routes.MapGet("/failed", FailedHandler.Get);
        routes.MapGet("/lagging", LaggingHandler.Get);
        routes.MapGet("/reservations", ReservationsHandler.Get);
        routes.MapGet("/retries", RetriesHandler.Get);
        routes.MapGet("/{groupName}/{name}", SubscriptionHandler.Get);
    }
}
