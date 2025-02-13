namespace Beckett.Dashboard.Subscriptions.Checkpoints.Lagging;

public static class LaggingEndpoint
{
    public static async Task<IResult> Handle(
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
