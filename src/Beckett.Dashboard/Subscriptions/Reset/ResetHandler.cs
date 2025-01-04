namespace Beckett.Dashboard.Subscriptions.Reset;

public static class ResetHandler
{
    public static async Task<IResult> Post(
        HttpContext context,
        string groupName,
        string name,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        await dashboard.Subscriptions.ResetSubscription(groupName, name, cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));
        context.Response.Headers.Append("HX-Trigger", new StringValues("subscription_reset"));

        return Results.Ok();
    }
}
