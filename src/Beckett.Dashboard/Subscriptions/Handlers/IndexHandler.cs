namespace Beckett.Dashboard.Subscriptions.Handlers;

public static class IndexHandler
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

        return Results.Extensions.Render<Index>(
            new Index.ViewModel(result.Subscriptions, null, pageParameter, pageSizeParameter, result.TotalResults)
        );
    }
}
