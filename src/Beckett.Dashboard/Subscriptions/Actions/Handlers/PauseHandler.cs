using Beckett.Database;
using Beckett.Subscriptions.Queries;

namespace Beckett.Dashboard.Subscriptions.Actions.Handlers;

public static class PauseHandler
{
    public static async Task<IResult> Post(
        HttpContext context,
        string groupName,
        string name,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        await database.Execute(new PauseSubscription(groupName, name, options), cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));

        return Results.Ok();
    }
}
