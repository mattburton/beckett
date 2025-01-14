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

        routes.MapGet("/{id:long}", GetCheckpointEndpoint.Handle);
        routes.MapPost("/{id:long}/release-reservation", ReleaseReservationEndpoint.Handle).DisableAntiforgery();
        routes.MapPost("/{id:long}/retry", RetryEndpoint.Handle).DisableAntiforgery();
        routes.MapPost("/{id:long}/skip", SkipEndpoint.Handle).DisableAntiforgery();
        routes.MapPost("/bulk-retry", BulkRetryEndpoint.Handle).DisableAntiforgery();
        routes.MapPost("/bulk-skip", BulkSkipEndpoint.Handle).DisableAntiforgery();
        routes.MapGet("/failed", GetFailedEndpoint.Handle);
        routes.MapGet("/lagging", GetLaggingEndpoint.Handle);
        routes.MapGet("/reservations", GetReservationsEndpoint.Handle);
        routes.MapGet("/retries", GetRetriesEndpoint.Handle);
    }
}
