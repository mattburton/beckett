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
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var decodedCategory = HttpUtility.UrlDecode(category);

        var result = await dashboard.MessageStore.GetCategoryStreams(decodedCategory, query, cancellationToken);

        return new Streams(new ViewModel(decodedCategory, query, result.Streams));
    }

    public record ViewModel(string Category, string? Query, IReadOnlyList<GetCategoryStreamsResult.Stream> Streams);
}
