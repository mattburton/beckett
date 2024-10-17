namespace Beckett.Dashboard.Subscriptions;

public static class FailedPage
{
    public static RouteGroupBuilder FailedPageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/subscriptions/failed", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(
        int? page,
        int? pageSize,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();

        var result = await dashboard.Subscriptions.GetFailed(pageParameter, pageSizeParameter, cancellationToken);

        return new Failed(new ViewModel(result.Failures, pageParameter, pageSizeParameter, result.TotalResults));
    }

    public record ViewModel(
        List<GetFailedResult.Failure> Failures,
        int Page,
        int PageSize,
        int TotalResults
    ) : IPagedViewModel;
}
