using System.Web;

namespace Beckett.Dashboard.MessageStore;

public static class StreamsPage
{
    public static RouteGroupBuilder StreamsRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/message-store/categories/{category}", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(
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

        return new Streams(
            new ViewModel(
                decodedCategory,
                query,
                result.Streams,
                page.ToPageParameter(),
                pageSize.ToPageSizeParameter(),
                result.TotalResults
            )
        );
    }

    public record ViewModel(
        string Category,
        string? Query,
        IReadOnlyList<GetCategoryStreamsResult.Stream> Streams,
        int Page,
        int PageSize,
        int TotalResults
    ) : IPagedViewModel;
}
