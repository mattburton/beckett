namespace Beckett.Messages.Scheduling;

public interface IMessageScheduler
{
    Task ScheduleMessages(
        string topic,
        string streamId,
        IEnumerable<ScheduledMessageEnvelope> messages,
        CancellationToken cancellationToken
    );
}
