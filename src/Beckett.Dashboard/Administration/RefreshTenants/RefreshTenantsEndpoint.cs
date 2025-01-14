namespace Beckett.Dashboard.Administration.RefreshTenants;

public static class RefreshTenantsEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        await dashboard.MessageStore.RefreshTenants(cancellationToken);

        context.Response.Headers.Append("HX-Refresh", new StringValues("true"));

        return Results.Ok();
    }
}
