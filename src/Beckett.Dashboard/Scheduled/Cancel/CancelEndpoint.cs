namespace Beckett.Dashboard.Scheduled.Cancel;

public static class CancelEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        Guid id,
        IMessageScheduler scheduler,
        CancellationToken cancellationToken
    )
    {
        await scheduler.CancelScheduledMessage(id, cancellationToken);

        context.Response.Headers.Append("HX-Trigger", new StringValues("scheduled_message_canceled"));
        context.Response.Headers.Append("HX-Redirect", new StringValues($"{Dashboard.Prefix}/scheduled"));

        return Results.Ok();
    }
}
