using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Pause;

public static class PauseEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        int id,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        await database.Execute(
            new Beckett.Subscriptions.Queries.PauseSubscription(id, options),
            cancellationToken
        );

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));

        return Results.Ok();
    }
}
