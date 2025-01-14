namespace Beckett.Dashboard.MessageStore.GetMessage;

public static class GetMessageByStreamPositionEndpoint
{
    public static async Task<IResult> Handle(
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
