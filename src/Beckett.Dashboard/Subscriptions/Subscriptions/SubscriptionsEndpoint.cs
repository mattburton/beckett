namespace Beckett.Dashboard.Subscriptions.Subscriptions;

public static class SubscriptionsEndpoint
{
    public static async Task<IResult> Handle(
        string? query,
        int? page,
        int? pageSize,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();

        var result = await dashboard.Subscriptions.GetSubscriptions(
            query,
            pageParameter,
            pageSizeParameter,
            cancellationToken
        );

        return Results.Extensions.Render<Subscriptions>(
            new Subscriptions.ViewModel(
                result.Subscriptions,
                query,
                pageParameter,
                pageSizeParameter,
                result.TotalResults
            )
        );
    }
}
