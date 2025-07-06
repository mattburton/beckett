using Beckett.Database;
using Beckett.Subscriptions.Queries;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.ReleaseReservation;

public static class ReleaseReservationEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        long id,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        await database.Execute(new ReleaseCheckpointReservation(id), cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("reservation_released"));

        return Results.Ok();
    }
}
