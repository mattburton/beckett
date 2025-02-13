namespace Beckett.Dashboard.Subscriptions.Checkpoints.Failed;

public static class FailedEndpoint
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

        var result = await dashboard.Subscriptions.GetFailed(
            query,
            pageParameter,
            pageSizeParameter,
            cancellationToken
        );

        return Results.Extensions.Render<Failed>(
            new Failed.ViewModel(
                result.Failures,
                query,
                pageParameter,
                pageSizeParameter,
                result.TotalResults
            )
        );
    }
}
