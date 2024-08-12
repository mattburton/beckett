using Beckett.Subscriptions.Retries;
using Microsoft.Extensions.Primitives;

namespace Beckett.Dashboard.Subscriptions.Actions;

public static class DeleteRetry
{
    public static RouteGroupBuilder DeleteRetryRoute(this RouteGroupBuilder builder)
    {
        builder.MapDelete("/subscriptions/retries/{id:guid}", Handler);

        return builder;
    }

    private static async Task<IResult> Handler(
        HttpContext context,
        Guid id,
        IRetryClient retryClient,
        CancellationToken cancellationToken
    )
    {
        await retryClient.DeleteRetry(id, cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("delete_requested"));

        return Results.Ok();
    }
}
