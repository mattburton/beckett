using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.Messages;

namespace Beckett.Database;

public class PostgresMessageStorage(
    IMessageSerializer messageSerializer,
    IPostgresDatabase database,
    IPostgresMessageDeserializer messageDeserializer
) : IMessageStorage
{
    public async Task<AppendResult> AppendToStream(
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

        return new AppendResult(streamVersion);
    }

    public async Task<ReadResult> ReadStream(
        string streamName,
        ReadOptions readOptions,
        AppendToStreamDelegate appendToStream,
        CancellationToken cancellationToken
    )
    {
        var streamMessages = await database.Execute(new ReadStream(streamName, readOptions), cancellationToken);

        var streamVersion = streamMessages.Count == 0 ? 0 : streamMessages[0].StreamVersion;

        var messages = streamMessages.Select(messageDeserializer.Deserialize).ToList();

        return new ReadResult(streamName, streamVersion, messages, appendToStream);
    }

    public async Task<IReadOnlyList<StreamChange>> ReadStreamChangeFeed(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var results = await database.Execute(
            new ReadStreamChangeFeed(lastGlobalPosition, batchSize),
            cancellationToken
        );

        return results.Count == 0
            ? []
            : results.Select(
                x => new StreamChange(
                    x.StreamName,
                    x.StreamVersion,
                    x.GlobalPosition,
                    x.MessageTypes
                )
            ).ToList();
    }
}
