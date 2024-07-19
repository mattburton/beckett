using System.Web;

namespace Beckett.Dashboard.MessageStore;

public static class MessagesPage
{
    public static RouteGroupBuilder MessagesRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/message-store/categories/{category}/{streamName}", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(
        string category,
        string streamName,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var decodedStreamName = HttpUtility.UrlDecode(streamName);

        var result = await dashboard.MessageStore.GetStreamMessages(decodedStreamName, cancellationToken);

        return new Messages(new ViewModel(category, decodedStreamName, result.Messages));
    }

    public record ViewModel(
        string Category,
        string StreamName,
        IReadOnlyList<GetStreamMessagesResult.Message> Messages);
}
