using Beckett.Database;
using Beckett.Subscriptions.Queries;

namespace Beckett.Dashboard.Subscriptions.Actions.Resume;

public static class ResumeHandler
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
        await database.Execute(new ResumeSubscription(groupName, name, options), cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));

        return Results.Ok();
    }
}
