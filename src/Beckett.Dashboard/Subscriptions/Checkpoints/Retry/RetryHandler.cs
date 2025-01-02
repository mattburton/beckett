using Beckett.Subscriptions.Retries;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Retry;

public static class RetryHandler
{
    public static async Task<IResult> Post(
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
