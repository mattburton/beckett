namespace Beckett.Dashboard.MessageStore;

public static class IndexPage
{
    public static RouteGroupBuilder IndexRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/message-store", Handler);

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

        var result = await dashboard.MessageStore.GetCategories(
            query,
            pageParameter,
            pageSizeParameter,
            cancellationToken
        );

        return new Index(
            new ViewModel(result.Categories, query, pageParameter, pageSizeParameter, result.TotalResults)
        );
    }

    public record ViewModel(
        List<GetCategoriesResult.Category> Categories,
        string? Query,
        int Page,
        int PageSize,
        int TotalResults
    ) : IPagedViewModel;
}
