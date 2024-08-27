using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.MessageStorage.Postgres.Queries;
using Microsoft.Extensions.Logging;

namespace Beckett.MessageStorage.Postgres;

public class PostgresMessageStorage(
    IMessageSerializer messageSerializer,
    IPostgresDatabase database,
    IPostgresMessageDeserializer messageDeserializer,
    IMessageTypeMap messageTypeMap,
    ILoggerFactory loggerFactory
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

    public async Task<ReadGlobalStreamResult> ReadGlobalStream(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var results = await database.Execute(new ReadGlobalStream(lastGlobalPosition, batchSize), cancellationToken);

        var items = new List<GlobalStreamItem>();

        if (results.Count <= 0)
        {
            return new ReadGlobalStreamResult(items);
        }

        foreach (var result in results)
        {
            var messageType = messageTypeMap.GetType(result.MessageType, loggerFactory);

            if (messageType == null)
            {
                continue;
            }

            var item = new GlobalStreamItem(
                result.StreamName,
                result.StreamPosition,
                result.GlobalPosition,
                messageType
            );

            items.Add(item);
        }

        return new ReadGlobalStreamResult(items);
    }

    public async Task<MessageStreamResult> ReadStream(
        string streamName,
        ReadStreamOptions readOptions,
        CancellationToken cancellationToken
    )
    {
        var streamMessages = await database.Execute(new ReadStream(streamName, readOptions), cancellationToken);

        var streamVersion = streamMessages.Count == 0 ? 0 : streamMessages[0].StreamVersion;

        var messages = new List<MessageResult>();

        foreach (var streamMessage in streamMessages)
        {
            var message = messageDeserializer.Deserialize(streamMessage);

            if (message is null)
            {
                continue;
            }

            messages.Add(message.Value);
        }

        return new MessageStreamResult(streamName, streamVersion, messages);
    }
}
