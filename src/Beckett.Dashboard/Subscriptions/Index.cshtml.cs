namespace Beckett.Dashboard.Subscriptions;

public static class IndexPage
{
    public static RouteGroupBuilder IndexPageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/subscriptions", Handler);

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

        var result = await dashboard.Subscriptions.GetSubscriptions(
            pageParameter,
            pageSizeParameter,
            cancellationToken
        );

        return new Index(
            new ViewModel(result.Subscriptions, null, pageParameter, pageSizeParameter, result.TotalResults)
        );
    }

    public record ViewModel(
        List<GetSubscriptionsResult.Subscription> Subscriptions,
        string? Query,
        int Page,
        int PageSize,
        int TotalResults
    ) : IPagedViewModel;
}
