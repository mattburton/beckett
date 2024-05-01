namespace Beckett.Events;

internal readonly struct ScheduledEvent(object @event, DateTimeOffset deliverAt)
{
    public object Event { get; } = @event;
    public DateTimeOffset DeliverAt { get; } = deliverAt;
}
