using Beckett.Dashboard.Subscriptions.Checkpoint;
using Beckett.Dashboard.Subscriptions.Failed;
using Beckett.Dashboard.Subscriptions.Lagging;
using Beckett.Dashboard.Subscriptions.Reservations;
using Beckett.Dashboard.Subscriptions.Retries;
using Beckett.Dashboard.Subscriptions.Subscription;
using Beckett.Dashboard.Subscriptions.Subscriptions;

namespace Beckett.Dashboard.Subscriptions;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/subscriptions");

        routes.MapGet("/", SubscriptionsHandler.Get);
        routes.MapGet("/checkpoints/{id:long}", CheckpointHandler.Get);
        routes.MapGet("/failed", FailedHandler.Get);
        routes.MapGet("/lagging", LaggingHandler.Get);
        routes.MapGet("/reservations", ReservationsHandler.Get);
        routes.MapGet("/retries", RetriesHandler.Get);
        routes.MapGet("/{groupName}/{name}", SubscriptionHandler.Get);
    }
}
