namespace Beckett.Dashboard.MessageStore.Handlers;

public static class StreamsHandler
{
    public static async Task<IResult> Get(
        string category,
        string? query,
        int? page,
        int? pageSize,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var decodedCategory = HttpUtility.UrlDecode(category);
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();

        var result = await dashboard.MessageStore.GetCategoryStreams(
            decodedCategory,
            query,
            pageParameter,
            pageSizeParameter,
            cancellationToken
        );

        return Results.Extensions.Render<Streams>(
            new Streams.ViewModel(
                decodedCategory,
                result.Streams,
                query,
                page.ToPageParameter(),
                pageSize.ToPageSizeParameter(),
                result.TotalResults
            )
        );
    }
}
