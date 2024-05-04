namespace Beckett.ScheduledEvents;

public readonly struct ScheduledEventWrapper(object @event, DateTimeOffset deliverAt)
{
    public object Event { get; } = @event;
    public DateTimeOffset DeliverAt { get; } = deliverAt;
}
