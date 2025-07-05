using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Replay;

public static class ReplayEndpoint
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
        await database.Execute(new ReplayQuery(groupName, name), cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("subscription_replay_started"));

        return Results.Ok();
    }
}
