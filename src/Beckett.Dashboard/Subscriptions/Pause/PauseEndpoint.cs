using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Pause;

public static class PauseEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        string groupName,
        string name,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        await database.Execute(new Beckett.Subscriptions.Queries.PauseSubscription(groupName, name, options), cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));

        return Results.Ok();
    }
}