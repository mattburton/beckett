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
        string topic,
        string streamId,
        ExpectedVersion expectedVersion,
        IEnumerable<MessageEnvelope> messages,
        CancellationToken cancellationToken
    )
    {
        var newMessages = messages.Select(x => MessageType.From(x.Message, x.Metadata, messageSerializer)).ToArray();

        var streamVersion = await database.Execute(
            new AppendToStream(topic, streamId, expectedVersion.Value, newMessages),
            cancellationToken
        );

        return new AppendResult(streamVersion);
    }

    public async Task<ReadResult> ReadStream(
        string topic,
        string streamId,
        ReadOptions readOptions,
        CancellationToken cancellationToken
    )
    {
        var streamMessages = await database.Execute(new ReadStream(topic, streamId, readOptions), cancellationToken);

        //TODO update query to always return actual stream version regardless of read options supplied
        var streamVersion = streamMessages.Count == 0 ? 0 : streamMessages[^1].StreamPosition;

        var messages = streamMessages.Select(messageDeserializer.Deserialize).ToList();

        return new ReadResult(messages, streamVersion);
    }

    public async Task<IReadOnlyList<StreamChange>> ReadStreamChanges(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var results = await database.Execute(
            new ReadStreamChanges(lastGlobalPosition, batchSize),
            cancellationToken
        );

        return results.Count == 0
            ? []
            : results.Select(x => new StreamChange(
                x.Topic,
                x.StreamId,
                x.StreamVersion,
                x.GlobalPosition,
                x.MessageTypes
            )).ToList();
    }
}
