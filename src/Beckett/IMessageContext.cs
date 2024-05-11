namespace Beckett;

public interface IMessageContext
{
    Guid Id { get; }
    string Topic { get; }
    string StreamId { get; }
    long StreamPosition { get; }
    long GlobalPosition { get; }
    Type Type { get; }
    object Message { get; }
    IDictionary<string, object> Metadata { get; }
    DateTimeOffset Timestamp { get; }
    IServiceProvider Services { get; }

    Task<AppendResult> AppendToStream(
        string topic,
        string streamId,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    );

    Task<ReadResult> ReadStream(
        string topic,
        string streamId,
        ReadOptions options,
        CancellationToken cancellationToken
    );
}

public static class MessageContextExtensions
{
    public static Task<AppendResult> AppendToStream(
        this IMessageContext context,
        string topic,
        string streamId,
        ExpectedVersion expectedVersion,
        object message,
        CancellationToken cancellationToken
    )
    {
        return context.AppendToStream(topic, streamId, expectedVersion, [message], cancellationToken);
    }

    public static Task<ReadResult> ReadStream(
        this IMessageContext context,
        string topic,
        string streamId,
        CancellationToken cancellationToken
    )
    {
        return context.ReadStream(topic, streamId, ReadOptions.Default, cancellationToken);
    }
}
