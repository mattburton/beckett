namespace Beckett.Messages.Scheduling;

public interface IMessageScheduler
{
    Task ScheduleMessages(
        string streamName,
        IEnumerable<ScheduledMessageEnvelope> messages,
        CancellationToken cancellationToken
    );
}
