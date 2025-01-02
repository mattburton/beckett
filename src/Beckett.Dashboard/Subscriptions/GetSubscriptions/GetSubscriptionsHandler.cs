namespace Beckett.Dashboard.Subscriptions.GetSubscriptions;

public static class GetSubscriptionsHandler
{
    public static async Task<IResult> Get(
        int? page,
        int? pageSize,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();

        var result = await dashboard.Subscriptions.GetSubscriptions(
            pageParameter,
            pageSizeParameter,
            cancellationToken
        );

        return Results.Extensions.Render<Subscriptions>(
            new Subscriptions.ViewModel(
                result.Subscriptions,
                null,
                pageParameter,
                pageSizeParameter,
                result.TotalResults
            )
        );
    }
}
