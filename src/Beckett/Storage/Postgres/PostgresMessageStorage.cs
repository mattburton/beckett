using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Storage.Postgres.Queries;

namespace Beckett.Storage.Postgres;

public class PostgresMessageStorage(IPostgresDataSource dataSource, IPostgresDatabase database)
    : IMessageStorage
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
            new AppendToStream(streamName, expectedVersion.Value, newMessages),
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

        var results = await database.Execute(new ReadGlobalStream(readOptions), connection, cancellationToken);

        var items = new List<GlobalStreamMessage>();

        if (results.Count <= 0)
        {
            return new ReadGlobalStreamResult(items);
        }

        items.AddRange(
            results.Select(result => new GlobalStreamMessage(
                    result.StreamName,
                    result.StreamPosition,
                    result.GlobalPosition,
                    result.MessageType,
                    result.Tenant,
                    result.Timestamp
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
        await using var connection = readOptions.RequirePrimary.GetValueOrDefault(false)
            ? dataSource.CreateMessageStoreWriteConnection()
            : dataSource.CreateMessageStoreReadConnection();

        await connection.OpenAsync(cancellationToken);

        var result = await database.Execute(
            new ReadStream(streamName, readOptions),
            cancellationToken
        );

        var messages = new List<StreamMessage>();

        foreach (var streamMessage in result.StreamMessages)
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

        return new ReadStreamResult(streamName, result.StreamVersion, messages);
    }
}
