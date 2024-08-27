using Beckett.Subscriptions.Retries;
using Microsoft.Extensions.Primitives;

namespace Beckett.Dashboard.Subscriptions.Actions;

public static class ManualRetry
{
    public static RouteGroupBuilder ManualRetryRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost("/subscriptions/retries/{id:guid}/manual-retry", Handler);

        return builder;
    }

    private static async Task<IResult> Handler(
        HttpContext context,
        Guid id,
        IRetryClient retryClient,
        CancellationToken cancellationToken
    )
    {
        await retryClient.ManualRetry(id, cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("manual_retry_requested"));

        return Results.Ok();
    }
}
