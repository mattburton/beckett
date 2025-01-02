namespace Beckett.Dashboard.MessageStore.GetMessage;

public static class GetMessageHandler
{
    public static async Task<IResult> GetById(
        string id,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var result = await dashboard.MessageStore.GetMessage(id, cancellationToken);

        return result is null ? Results.NotFound() : Results.Extensions.Render<Message>(new Message.ViewModel(result));
    }

    public static async Task<IResult> GetByStreamPosition(
        string streamName,
        long streamPosition,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var decodedStreamName = HttpUtility.UrlDecode(streamName);

        var result = await dashboard.MessageStore.GetMessage(decodedStreamName, streamPosition, cancellationToken);

        return result is null ? Results.NotFound() : Results.Extensions.Render<Message>(new Message.ViewModel(result));
    }
}
