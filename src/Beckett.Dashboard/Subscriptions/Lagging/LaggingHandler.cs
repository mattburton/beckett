namespace Beckett.Dashboard.Subscriptions.Lagging;

public static class LaggingHandler
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

        var result = await dashboard.Subscriptions.GetLaggingSubscriptions(
            pageParameter,
            pageSizeParameter,
            cancellationToken
        );

        return Results.Extensions.Render<Lagging>(
            new Lagging.ViewModel(result.Subscriptions, null, pageParameter, pageSizeParameter, result.TotalResults)
        );
    }
}
