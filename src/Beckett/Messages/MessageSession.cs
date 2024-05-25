using Beckett.OpenTelemetry;

namespace Beckett.Messages;

public class MessageSession(
    IMessageStore messageStore,
    IInstrumentation instrumentation,
    IMessageStorage messageStorage
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

        var messages = new List<SessionMessageEnvelope>();

        var metadata = new Dictionary<string, object>();

        using var activity = instrumentation.StartSaveChangesActivity(metadata);

        foreach (var pendingMessage in _pendingMessages)
        {
            messages.Add(
                new SessionMessageEnvelope(
                    pendingMessage.StreamName,
                    pendingMessage.Message,
                    metadata,
                    streamVersions[pendingMessage.StreamName]
                )
            );

            if (pendingMessage.ExpectedVersion >= 0)
            {
                streamVersions[pendingMessage.StreamName]++;
            }
        }

        await messageStorage.SaveChanges(messages, cancellationToken);

        _pendingMessages.Clear();
    }

    private readonly record struct PendingMessage(
        string StreamName,
        object Message,
        long ExpectedVersion
    );
}
