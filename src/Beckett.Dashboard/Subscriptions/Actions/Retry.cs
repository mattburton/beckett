using Beckett.Subscriptions.Retries;
using Microsoft.Extensions.Primitives;

namespace Beckett.Dashboard.Subscriptions.Actions;

public static class Retry
{
    public static RouteGroupBuilder ManualRetryRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost("/subscriptions/checkpoints/{id:long}/retry", Handler);

        return builder;
    }

    private static async Task<IResult> Handler(
        HttpContext context,
        long id,
        IRetryClient retryClient,
        CancellationToken cancellationToken
    )
    {
        await retryClient.Retry(id, cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("retry_requested"));

        return Results.Ok();
    }
}
