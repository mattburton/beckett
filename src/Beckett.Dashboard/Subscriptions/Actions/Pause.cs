using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Microsoft.Extensions.Primitives;

namespace Beckett.Dashboard.Subscriptions.Actions;

public static class Pause
{
    public static RouteGroupBuilder PauseRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost("/subscriptions/{groupName}/{name}/pause", Handler).DisableAntiforgery();

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
        await database.Execute(new PauseSubscription(groupName, name, options), cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));

        return Results.Ok();
    }
}
