using Beckett.Subscriptions.Retries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Beckett.Dashboard.Subscriptions.Actions;

public static class BulkDelete
{
    public static RouteGroupBuilder BulkDeleteRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost("/subscriptions/retries/bulk-delete", Handler).DisableAntiforgery();

        return builder;
    }

    private static async Task<IResult> Handler(
        HttpContext context,
        [FromForm(Name = "id")] Guid[] retryIds,
        IRetryClient retryClient,
        CancellationToken cancellationToken
    )
    {
        await retryClient.BulkDelete(retryIds, cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("bulk_delete_requested"));

        return Results.Ok();
    }

    public record Request(List<Guid> RetryIds);
}
