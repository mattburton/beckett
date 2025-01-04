using Beckett.Dashboard.Subscriptions.Checkpoints.BulkRetry;
using Beckett.Dashboard.Subscriptions.Checkpoints.BulkSkip;
using Beckett.Dashboard.Subscriptions.Checkpoints.GetCheckpoint;
using Beckett.Dashboard.Subscriptions.Checkpoints.GetFailed;
using Beckett.Dashboard.Subscriptions.Checkpoints.GetLagging;
using Beckett.Dashboard.Subscriptions.Checkpoints.GetReservations;
using Beckett.Dashboard.Subscriptions.Checkpoints.GetRetries;
using Beckett.Dashboard.Subscriptions.Checkpoints.ReleaseReservation;
using Beckett.Dashboard.Subscriptions.Checkpoints.Retry;
using Beckett.Dashboard.Subscriptions.Checkpoints.Skip;

namespace Beckett.Dashboard.Subscriptions.Checkpoints;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/subscriptions/checkpoints");

        routes.MapGet("/{id:long}", GetCheckpointHandler.Get);
        routes.MapPost("/{id:long}/release-reservation", ReleaseReservationHandler.Post).DisableAntiforgery();
        routes.MapPost("/{id:long}/retry", RetryHandler.Post).DisableAntiforgery();
        routes.MapPost("/{id:long}/skip", SkipHandler.Post).DisableAntiforgery();
        routes.MapPost("/bulk-retry", BulkRetryHandler.Post).DisableAntiforgery();
        routes.MapPost("/bulk-skip", BulkSkipHandler.Post).DisableAntiforgery();
        routes.MapGet("/failed", GetFailedHandler.Get);
        routes.MapGet("/lagging", GetLaggingHandler.Get);
        routes.MapGet("/reservations", GetReservationsHandler.Get);
        routes.MapGet("/retries", GetRetriesHandler.Get);
    }
}
