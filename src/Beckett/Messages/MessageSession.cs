using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;
using Beckett.OpenTelemetry;

namespace Beckett.Messages;

public class MessageSession(
    IMessageStore messageStore,
    IInstrumentation instrumentation,
    IMessageSerializer messageSerializer,
    IPostgresDatabase database
) : IMessageSession
{
    private readonly List<PendingMessage> _pendingMessages = [];

    public void AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages
    )
    {
        foreach (var message in messages)
        {
            _pendingMessages.Add(new PendingMessage(streamName, message, expectedVersion.Value));
        }
    }

    public Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken) =>
        messageStore.ReadStream(streamName, options, cancellationToken);

    public async Task SaveChanges(CancellationToken cancellationToken)
    {
        if (_pendingMessages.Count == 0)
        {
            return;
        }

        var streamVersions = _pendingMessages.GroupBy(x => x.StreamName).ToDictionary(
            key => key.Key,
            value => value.First().ExpectedVersion
        );

        var messages = new List<MessageType>();

        var metadata = new Dictionary<string, object>();

        using var activity = instrumentation.StartSaveChangesActivity(metadata);

        foreach (var pendingMessage in _pendingMessages)
        {
            var messageId = Guid.NewGuid();

            var (_, typeName, data, serializedMetadata) = messageSerializer.Serialize(pendingMessage.Message, metadata);

            messages.Add(
                new MessageType
                {
                    Id = messageId,
                    StreamName = pendingMessage.StreamName,
                    Type = typeName,
                    Data = data,
                    Metadata = serializedMetadata,
                    ExpectedVersion = streamVersions[pendingMessage.StreamName]
                }
            );

            if (pendingMessage.ExpectedVersion >= 0)
            {
                streamVersions[pendingMessage.StreamName]++;
            }
        }

        await database.Execute(new AppendMessages(messages.ToArray()), cancellationToken);

        _pendingMessages.Clear();
    }

    private readonly record struct PendingMessage(
        string StreamName,
        object Message,
        long ExpectedVersion
    );
}
