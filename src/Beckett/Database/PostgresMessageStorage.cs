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
        var newMessages = messages.Select(x => MessageType.From(x.Message, x.Metadata, messageSerializer)).ToArray();

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

    public async Task<ReadStreamChangeFeedResult> ReadStreamChangeFeed(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var results = await database.Execute(
            new ReadStreamChangeFeed(lastGlobalPosition, batchSize),
            cancellationToken
        );

        var items = results.Count == 0
            ? []
            : results.Select(
                x => new StreamChange(
                    x.StreamName,
                    x.StreamVersion,
                    x.GlobalPosition,
                    x.MessageTypes.Select(
                        t => messageTypeMap.GetType(t) ?? throw new Exception($"Unknown message type: {t}")
                    ).ToArray()
                )
            ).ToList();

        return new ReadStreamChangeFeedResult(items);
    }
}
