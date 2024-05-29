namespace Beckett;

public interface IMessageScheduler
{
    Task Cancel(Guid id, CancellationToken cancellationToken);

    Task<Guid> Schedule(
        string streamName,
        object message,
        TimeSpan delay,
        CancellationToken cancellationToken
    ) => Schedule(streamName, message, DateTimeOffset.UtcNow.Add(delay), cancellationToken);


    Task<Guid> Schedule(
        string streamName,
        object message,
        DateTimeOffset deliverAt,
        CancellationToken cancellationToken
    );
}
