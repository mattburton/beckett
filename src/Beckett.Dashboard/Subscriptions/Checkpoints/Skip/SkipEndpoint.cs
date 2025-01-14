using Beckett.Subscriptions.Retries;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Skip;

public static class SkipEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        long id,
        IRetryClient retryClient,
        CancellationToken cancellationToken
    )
    {
        await retryClient.Skip(id, cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("checkpoint_skipped"));

        return Results.Ok();
    }
}
