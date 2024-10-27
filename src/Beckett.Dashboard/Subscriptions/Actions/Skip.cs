using Beckett.Subscriptions.Retries;
using Microsoft.Extensions.Primitives;

namespace Beckett.Dashboard.Subscriptions.Actions;

public static class Skip
{
    public static RouteGroupBuilder SkipRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost("/subscriptions/checkpoints/{id:long}/skip", Handler);

        return builder;
    }

    private static async Task<IResult> Handler(
        HttpContext context,
        long id,
        IRetryClient retryClient,
        CancellationToken cancellationToken
    )
    {
        await retryClient.Skip(id, cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("checkpoint_skipped"));

        return Results.Ok();
    }
}
