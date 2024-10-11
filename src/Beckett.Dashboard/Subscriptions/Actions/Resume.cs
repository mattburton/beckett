using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Primitives;

namespace Beckett.Dashboard.Subscriptions.Actions;

public static class Resume
{
    public static RouteGroupBuilder ResumeRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost("/subscriptions/{groupName}/{name}/resume", Handler).DisableAntiforgery();

        return builder;
    }

    private static async Task<IResult> Handler(
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
