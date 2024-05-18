namespace Beckett;

public interface IMessageScheduler
{
    Task Schedule(
        string streamName,
        object message,
        TimeSpan delay,
        CancellationToken cancellationToken
    ) => Schedule(streamName, [message], delay, cancellationToken);

    Task Schedule(
        string streamName,
        IEnumerable<object> messages,
        TimeSpan delay,
        CancellationToken cancellationToken
    ) => Schedule(streamName, messages, DateTimeOffset.UtcNow.Add(delay), cancellationToken);

    Task Schedule(
        string streamName,
        object message,
        DateTimeOffset deliverAt,
        CancellationToken cancellationToken
    ) => Schedule(streamName, [message], deliverAt, cancellationToken);

    Task Schedule(
        string streamName,
        IEnumerable<object> messages,
        DateTimeOffset deliverAt,
        CancellationToken cancellationToken
    );
}
