namespace Beckett.Dashboard.Subscriptions;

public static class LaggingPage
{
    public static RouteGroupBuilder LaggingPageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/subscriptions/lagging", Handler);

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

        var result = await dashboard.Subscriptions.GetLaggingSubscriptions(
            pageParameter,
            pageSizeParameter,
            cancellationToken
        );

        return new Lagging(new ViewModel(result.Subscriptions, pageParameter, pageSizeParameter, result.TotalResults));
    }

    public record ViewModel(
        List<GetLaggingSubscriptionsResult.Subscription> Subscriptions,
        int Page,
        int PageSize,
        int TotalResults
    ) : IPagedViewModel;
}
