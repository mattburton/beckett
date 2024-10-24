namespace Beckett.Dashboard.Subscriptions;

public static class RetriesPage
{
    public static RouteGroupBuilder RetriesPageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/subscriptions/retries", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(
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

        return new Retries(new ViewModel(result.Retries, query, pageParameter, pageSizeParameter, result.TotalResults));
    }

    public record ViewModel(
        List<GetRetriesResult.Retry> Retries,
        string? Query,
        int Page,
        int PageSize,
        int TotalResults
    ) : IPagedViewModel;
}
