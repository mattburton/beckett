using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Reset;

public static class ResetEndpoint
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
        await database.Execute(new ResetQuery(groupName, name, options), cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("subscription_reset"));

        return Results.Ok();
    }
}
