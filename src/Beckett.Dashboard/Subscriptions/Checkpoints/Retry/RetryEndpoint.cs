using Beckett.Dashboard.Subscriptions.Checkpoints.Shared.Queries;
using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Retry;

public static class RetryEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        long id,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        await database.Execute(new ScheduleCheckpoints([id], DateTimeOffset.UtcNow, options), cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("retry_requested"));

        return Results.Ok();
    }
}
