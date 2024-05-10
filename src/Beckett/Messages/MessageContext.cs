namespace Beckett.Messages;

public readonly record struct MessageContext(
    Guid Id,
    string StreamName,
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
    public Task<AppendResult> AppendToStream(string streamName, ExpectedVersion expectedVersion, IEnumerable<object> messages,
        CancellationToken cancellationToken)
    {
        return MessageStore.AppendToStream(streamName, expectedVersion, messages, cancellationToken);
    }

    public Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken)
    {
        return MessageStore.ReadStream(streamName, options, cancellationToken);
    }
}
