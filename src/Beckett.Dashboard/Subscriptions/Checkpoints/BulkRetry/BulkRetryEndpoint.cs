using Beckett.Dashboard.Subscriptions.Checkpoints.Shared.Queries;
using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.BulkRetry;

public static class BulkRetryEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        [FromForm(Name = "id")] long[] ids,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        await database.Execute(new ScheduleCheckpoints(ids, DateTimeOffset.UtcNow, options), cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("bulk_retry_requested"));

        return Results.Ok();
    }
}
