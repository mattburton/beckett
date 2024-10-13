using Beckett.Database;
using Beckett.Database.Types;
using Beckett.MessageStorage.Postgres.Queries;

namespace Beckett.MessageStorage.Postgres;

public class PostgresMessageStorage(IPostgresDatabase database, PostgresOptions options) : IMessageStorage
{
    public async Task<AppendToStreamResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IReadOnlyList<Message> messages,
        CancellationToken cancellationToken
    )
    {
        var newMessages = messages.Select(x => MessageType.From(streamName, x)).ToArray();

        var streamVersion = await database.Execute(
            new AppendToStream(streamName, expectedVersion.Value, newMessages, options),
            cancellationToken
        );

        return new AppendToStreamResult(streamVersion);
    }

    public async Task<ReadGlobalStreamResult> ReadGlobalStream(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var results = await database.Execute(
            new ReadGlobalStream(lastGlobalPosition, batchSize, options),
            cancellationToken
        );

        var items = new List<GlobalStreamItem>();

        if (results.Count <= 0)
        {
            return new ReadGlobalStreamResult(items);
        }

        items.AddRange(
            results.Select(
                result => new GlobalStreamItem(
                    result.StreamName,
                    result.StreamPosition,
                    result.GlobalPosition,
                    result.MessageType
                )
            )
        );

        return new ReadGlobalStreamResult(items);
    }

    public async Task<ReadStreamResult> ReadStream(
        string streamName,
        ReadStreamOptions readOptions,
        CancellationToken cancellationToken
    )
    {
        var streamMessages = await database.Execute(
            new ReadStream(streamName, readOptions, options),
            cancellationToken
        );

        var streamVersion = streamMessages.Count == 0 ? 0 : streamMessages[0].StreamVersion;

        var messages = new List<StreamMessage>();

        foreach (var streamMessage in streamMessages)
        {
            messages.Add(
                new StreamMessage(
                    streamMessage.Id.ToString(),
                    streamMessage.StreamName,
                    streamMessage.StreamPosition,
                    streamMessage.GlobalPosition,
                    streamMessage.Type,
                    streamMessage.Data,
                    streamMessage.Metadata,
                    streamMessage.Timestamp
                )
            );
        }

        return new ReadStreamResult(streamName, streamVersion, messages);
    }
}
