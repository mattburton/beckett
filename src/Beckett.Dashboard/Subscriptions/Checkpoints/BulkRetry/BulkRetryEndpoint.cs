using Beckett.Subscriptions.Retries;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.BulkRetry;

public static class BulkRetryEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        [FromForm(Name = "id")] long[] ids,
        IRetryClient retryClient,
        CancellationToken cancellationToken
    )
    {
        await retryClient.BulkRetry(ids, cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("bulk_retry_requested"));

        return Results.Ok();
    }
}
