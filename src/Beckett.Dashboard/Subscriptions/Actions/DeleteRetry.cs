using Beckett.Subscriptions.Retries;
using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.Primitives;

namespace Beckett.Dashboard.Subscriptions.Actions;

public static class DeleteRetry
{
    public static RouteGroupBuilder DeleteRetryRoute(this RouteGroupBuilder builder)
    {
        builder.MapDelete("/subscriptions/retries/{id:guid}", Handler);

        return builder;
    }

    private static async Task<IResult> Handler(HttpContext context, Guid id, IMessageStore messageStore, CancellationToken cancellationToken)
    {
        await messageStore.AppendToStream(
            RetryStreamName.For(id),
            ExpectedVersion.Any,
            new DeleteRetryRequested(id, DateTimeOffset.UtcNow),
            cancellationToken
        );

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));

        return Results.Ok();
    }
}
