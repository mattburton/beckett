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
        await retryClient.RequestManualRetry(id, cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));

        return Results.Ok();
    }
}
