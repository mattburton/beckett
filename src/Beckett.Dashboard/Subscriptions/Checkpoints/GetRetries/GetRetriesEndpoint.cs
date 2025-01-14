namespace Beckett.Dashboard.Subscriptions.Checkpoints.GetRetries;

public static class GetRetriesEndpoint
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

        var result = await dashboard.Subscriptions.GetRetries(
            query,
            pageParameter,
            pageSizeParameter,
            cancellationToken
        );

        return Results.Extensions.Render<Retries>(
            new Retries.ViewModel(
                result.Retries,
                query,
                pageParameter,
                pageSizeParameter,
                result.TotalResults
            )
        );
    }
}
