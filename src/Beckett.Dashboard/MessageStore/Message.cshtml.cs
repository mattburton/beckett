using System.Web;

namespace Beckett.Dashboard.MessageStore;

public static class MessagePage
{
    public static RouteGroupBuilder MessageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/message-store/messages/{id}", MessageByIdHandler);

        builder.MapGet("/message-store/streams/{streamName}/{streamPosition:long}", MessageByStreamPositionHandler);

        return builder;
    }

    public static async Task<IResult> MessageByIdHandler(
        string id,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var result = await dashboard.MessageStore.GetMessage(id, cancellationToken);

        return result is null ? Results.NotFound() : new Message(new ViewModel(result));
    }

    public static async Task<IResult> MessageByStreamPositionHandler(
        string streamName,
        long streamPosition,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var decodedStreamName = HttpUtility.UrlDecode(streamName);

        var result = await dashboard.MessageStore.GetMessage(decodedStreamName, streamPosition, cancellationToken);

        return result is null ? Results.NotFound() : new Message(new ViewModel(result));
    }

    public record ViewModel(GetMessageResult Message);
}
