namespace Beckett.Messages;

public readonly record struct MessageContext(
    Guid Id,
    string Topic,
    string StreamId,
    long StreamPosition,
    long GlobalPosition,
    Type Type,
    object Message,
    IDictionary<string, object> Metadata,
    DateTimeOffset Timestamp,
    IMessageStore MessageStore,
    IServiceProvider Services
) : IMessageContext
{
    public Task<AppendResult> AppendToStream(
        string topic,
        string streamId,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    )
    {
        return MessageStore.AppendToStream(topic, streamId, expectedVersion, messages, cancellationToken);
    }

    public Task<ReadResult> ReadStream(string topic, string streamId, ReadOptions options, CancellationToken cancellationToken)
    {
        return MessageStore.ReadStream(topic, streamId, options, cancellationToken);
    }
}
