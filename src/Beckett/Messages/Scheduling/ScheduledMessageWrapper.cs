namespace Beckett.Messages.Scheduling;

public readonly struct ScheduledMessageWrapper(object message, DateTimeOffset deliverAt)
{
    public object Message { get; } = message;
    public DateTimeOffset DeliverAt { get; } = deliverAt;
}
