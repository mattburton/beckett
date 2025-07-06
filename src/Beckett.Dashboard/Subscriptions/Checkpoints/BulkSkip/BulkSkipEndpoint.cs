using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.BulkSkip;

public static class BulkSkipEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        [FromForm(Name = "id")] long[] ids,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        await database.Execute(new BulkSkipQuery(ids), cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("bulk_skip_requested"));

        return Results.Ok();
    }
}
