using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Resume;

public static class ResumeEndpoint
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
        await database.Execute(new ResumeQuery(groupName, name), cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));

        return Results.Ok();
    }
}
