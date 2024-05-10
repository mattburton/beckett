namespace Beckett;

public interface IMessageContext
{
    Guid Id { get; }
    string StreamName { get; }
    long StreamPosition { get; }
    long GlobalPosition { get; }
    Type Type { get; }
    object Message { get; }
    IDictionary<string, object> Metadata { get; }
    DateTimeOffset Timestamp { get; }
    IServiceProvider Services { get; }

    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    );

    Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken);
}

public static class MessageContextExtensions
{
    public static Task<AppendResult> AppendToStream(
        this IMessageContext context,
        string streamName,
        ExpectedVersion expectedVersion,
        object message,
        CancellationToken cancellationToken
    )
    {
        return context.AppendToStream(streamName, expectedVersion, [message], cancellationToken);
    }

    public static Task<ReadResult> ReadStream(
        this IMessageContext context,
        string streamName,
        CancellationToken cancellationToken
    )
    {
        return context.ReadStream(streamName, ReadOptions.Default, cancellationToken);
    }
}
