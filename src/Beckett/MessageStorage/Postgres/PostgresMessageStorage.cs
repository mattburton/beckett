using Beckett.Database;
using Beckett.Database.Types;
using Beckett.MessageStorage.Postgres.Queries;

namespace Beckett.MessageStorage.Postgres;

public class PostgresMessageStorage(
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    PostgresOptions options) : IMessageStorage
{
    public async Task<AppendToStreamResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IReadOnlyList<Message> messages,
        CancellationToken cancellationToken
    )
    {
        var newMessages = messages.Select(x => MessageType.From(streamName, x)).ToArray();

        await using var connection = dataSource.CreateMessageStoreWriteConnection();

        await connection.OpenAsync(cancellationToken);

        var streamVersion = await database.Execute(
            new AppendToStream(streamName, expectedVersion.Value, newMessages, options),
            connection,
            cancellationToken
        );

        return new AppendToStreamResult(streamVersion);
    }

    public async Task<ReadGlobalStreamResult> ReadGlobalStream(
        ReadGlobalStreamOptions readOptions,
        CancellationToken cancellationToken
    )
    {
        await using var connection = dataSource.CreateMessageStoreReadConnection();

        await connection.OpenAsync(cancellationToken);

        var streamMessages = await database.Execute(
            new ReadGlobalStream(readOptions, options),
            connection,
            cancellationToken
        );

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

        return new ReadGlobalStreamResult(messages);
    }

    public async Task<ReadIndexBatchResult> ReadIndexBatch(
        ReadIndexBatchOptions readOptions,
        CancellationToken cancellationToken
    )
    {
        await using var connection = dataSource.CreateMessageStoreReadConnection();

        await connection.OpenAsync(cancellationToken);

        var results = await database.Execute(
            new ReadIndexBatch(readOptions, options),
            connection,
            cancellationToken
        );

        var items = new List<IndexBatchItem>();

        if (results.Count <= 0)
        {
            return new ReadIndexBatchResult(items);
        }

        items.AddRange(
            results.Select(result => new IndexBatchItem(
                    result.StreamName,
                    result.StreamPosition,
                    result.GlobalPosition,
                    result.MessageType,
                    result.Tenant,
                    result.Timestamp
                )
            )
        );

        return new ReadIndexBatchResult(items);
    }

    public async Task<ReadStreamResult> ReadStream(
        string streamName,
        ReadStreamOptions readOptions,
        CancellationToken cancellationToken
    )
    {
        await using var connection = readOptions.RequirePrimary.GetValueOrDefault(false)
            ? dataSource.CreateMessageStoreWriteConnection()
            : dataSource.CreateMessageStoreReadConnection();

        await connection.OpenAsync(cancellationToken);

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
