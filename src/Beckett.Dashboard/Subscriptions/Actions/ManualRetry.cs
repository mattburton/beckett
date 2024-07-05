using Beckett.Subscriptions.Retries;
using Beckett.Subscriptions.Retries.Events;
using Microsoft.Extensions.Primitives;

namespace Beckett.Dashboard.Subscriptions.Actions;

public static class ManualRetry
{
    public static RouteGroupBuilder ManualRetryRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost("/subscriptions/retries/{id:guid}/manual-retry", Handler);

        return builder;
    }

    private static async Task<IResult> Handler(HttpContext context, Guid id, IMessageStore messageStore, CancellationToken cancellationToken)
    {
        await messageStore.AppendToStream(
            RetryStreamName.For(id),
            ExpectedVersion.Any,
            new ManualRetryRequested(id, DateTimeOffset.UtcNow),
            cancellationToken
        );

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));

        return Results.Ok();
    }
}
