using Beckett.Subscriptions.Retries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Beckett.Dashboard.Subscriptions.Actions;

public static class BulkRetry
{
    public static RouteGroupBuilder BulkRetryRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost("/subscriptions/retries/bulk-retry", Handler).DisableAntiforgery();

        return builder;
    }

    private static async Task<IResult> Handler(
        HttpContext context,
        [FromForm(Name = "id")] Guid[] retryIds,
        IRetryClient retryClient,
        CancellationToken cancellationToken
    )
    {
        await retryClient.BulkRetry(retryIds, cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("bulk_retry_requested"));

        return Results.Ok();
    }
}
