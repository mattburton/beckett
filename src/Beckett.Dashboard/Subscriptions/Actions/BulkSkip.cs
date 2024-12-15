using Beckett.Subscriptions.Retries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Beckett.Dashboard.Subscriptions.Actions;

public static class BulkSkip
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
