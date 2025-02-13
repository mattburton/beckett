using Beckett.Dashboard.Subscriptions.Checkpoints.BulkRetry;
using Beckett.Dashboard.Subscriptions.Checkpoints.BulkSkip;
using Beckett.Dashboard.Subscriptions.Checkpoints.Checkpoint;
using Beckett.Dashboard.Subscriptions.Checkpoints.Failed;
using Beckett.Dashboard.Subscriptions.Checkpoints.Lagging;
using Beckett.Dashboard.Subscriptions.Checkpoints.ReleaseReservation;
using Beckett.Dashboard.Subscriptions.Checkpoints.Reservations;
using Beckett.Dashboard.Subscriptions.Checkpoints.Retries;
using Beckett.Dashboard.Subscriptions.Checkpoints.Retry;
using Beckett.Dashboard.Subscriptions.Checkpoints.Skip;

namespace Beckett.Dashboard.Subscriptions.Checkpoints;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/subscriptions/checkpoints");

        routes.MapGet("/{id:long}", CheckpointEndpoint.Handle);
        routes.MapPost("/{id:long}/release-reservation", ReleaseReservationEndpoint.Handle).DisableAntiforgery();
        routes.MapPost("/{id:long}/retry", RetryEndpoint.Handle).DisableAntiforgery();
        routes.MapPost("/{id:long}/skip", SkipEndpoint.Handle).DisableAntiforgery();
        routes.MapPost("/bulk-retry", BulkRetryEndpoint.Handle).DisableAntiforgery();
        routes.MapPost("/bulk-skip", BulkSkipEndpoint.Handle).DisableAntiforgery();
        routes.MapGet("/failed", FailedEndpoint.Handle);
        routes.MapGet("/lagging", LaggingEndpoint.Handle);
        routes.MapGet("/reservations", ReservationsEndpoint.Handle);
        routes.MapGet("/retries", RetriesEndpoint.Handle);
    }
}
