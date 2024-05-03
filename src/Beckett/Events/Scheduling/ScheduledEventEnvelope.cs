namespace Beckett.Events.Scheduling;

public readonly struct ScheduledEventEnvelope(object @event, Dictionary<string, object> metadata, DateTimeOffset deliverAt)
{
    public object Event { get; } = @event;
    public Dictionary<string, object> Metadata { get; } = metadata;
    public DateTimeOffset DeliverAt { get; } = deliverAt;
}
