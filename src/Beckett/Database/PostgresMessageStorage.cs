using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.Messages.Storage;

namespace Beckett.Database;

public class PostgresMessageStorage(
    IMessageSerializer messageSerializer,
    IPostgresDatabase database,
    IPostgresMessageDeserializer messageDeserializer,
    IMessageTypeMap messageTypeMap
) : IMessageStorage
{
    public async Task<AppendToStreamResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<MessageEnvelope> messages,
        CancellationToken cancellationToken
    )
    {
        var newMessages = messages.Select(x => MessageType.From(streamName, x.Message, x.Metadata, messageSerializer))
            .ToArray();

        var streamVersion = await database.Execute(
            new AppendToStream(streamName, expectedVersion.Value, newMessages),
            cancellationToken
        );

        return new AppendToStreamResult(streamVersion);
    }

    public async Task<ReadStreamResult> ReadStream(
        string streamName,
        ReadStreamOptions readOptions,
        CancellationToken cancellationToken
    )
    {
        var streamMessages = await database.Execute(new ReadStream(streamName, readOptions), cancellationToken);

        var streamVersion = streamMessages.Count == 0 ? 0 : streamMessages[0].StreamVersion;

        var messages = streamMessages.Select(messageDeserializer.Deserialize).ToList();

        return new ReadStreamResult(streamName, streamVersion, messages);
    }

    public async Task<ReadGlobalStreamResult> ReadGlobalStream(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var results = await database.Execute(new ReadGlobalStream(lastGlobalPosition, batchSize), cancellationToken);

        var items = results.Count == 0
            ? []
            : results.Select(
                x => new GlobalStreamItem(
                    x.StreamName,
                    x.StreamPosition,
                    x.GlobalPosition,
                    messageTypeMap.GetType(x.MessageType) ??
                    throw new Exception($"Unknown message type: {x.MessageType}")
                )
            ).ToList();

        return new ReadGlobalStreamResult(items);
    }
}
