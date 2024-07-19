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
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var decodedCategory = HttpUtility.UrlDecode(category);

        var result = await dashboard.MessageStore.GetCategoryStreams(decodedCategory, cancellationToken);

        return new Streams(new ViewModel(decodedCategory, result.Streams));
    }

    public record ViewModel(string Category, IReadOnlyList<GetCategoryStreamsResult.Stream> Streams);
}
