namespace Beckett.Events;

public interface IEventStorage
{
    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<EventEnvelope> events,
        CancellationToken cancellationToken
    );

    IEnumerable<Task> ConfigureBackgroundService(CancellationToken stoppingToken);

    Task DeliverScheduledEvents(CancellationToken cancellationToken);

    Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken);
}
