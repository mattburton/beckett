using Beckett.Database;

namespace Beckett.Dashboard.MessageStore.Message;

public static class MessageByStreamPositionEndpoint
{
    public static async Task<IResult> Handle(
        string streamName,
        long streamPosition,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        var decodedStreamName = HttpUtility.UrlDecode(streamName);

        var result = await database.Execute(
            new MessageByStreamPositionQuery(decodedStreamName, streamPosition),
            cancellationToken
        );

        return result is null ? Results.NotFound() : Results.Extensions.Render<Message>(new Message.ViewModel(result));
    }
}
