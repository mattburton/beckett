using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Skip;

public static class SkipEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        long id,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        await database.Execute(new SkipQuery(id), cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("checkpoint_skipped"));

        return Results.Ok();
    }
}
