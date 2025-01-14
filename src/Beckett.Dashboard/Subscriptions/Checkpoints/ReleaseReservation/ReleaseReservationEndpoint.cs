namespace Beckett.Dashboard.Subscriptions.Checkpoints.ReleaseReservation;

public static class ReleaseReservationEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        long id,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        await dashboard.Subscriptions.ReleaseCheckpointReservation(id, cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("reservation_released"));

        return Results.Ok();
    }
}
