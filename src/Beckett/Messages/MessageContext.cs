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
    IMessageScheduler MessageScheduler,
    IServiceProvider Services
) : IMessageContext
{
    public Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    ) => MessageStore.AppendToStream(streamName, expectedVersion, messages, cancellationToken);

    public Task<ReadResult> ReadStream(
        string streamName,
        ReadOptions options,
        CancellationToken cancellationToken
    ) => MessageStore.ReadStream(streamName, options, cancellationToken);

    public Task Schedule(
        string streamName,
        IEnumerable<object> messages,
        DateTimeOffset deliverAt,
        CancellationToken cancellationToken
    ) => MessageScheduler.Schedule(streamName, messages, deliverAt, cancellationToken);
}
