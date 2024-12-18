using Beckett.Subscriptions.Retries;

namespace Beckett.Dashboard.Subscriptions.Actions.Handlers;

public static class BulkSkipHandler
{
    public static async Task<IResult> Post(
        HttpContext context,
        [FromForm(Name = "id")] long[] ids,
        IRetryClient retryClient,
        CancellationToken cancellationToken
    )
    {
        await retryClient.BulkSkip(ids, cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("bulk_skip_requested"));

        return Results.Ok();
    }
}
