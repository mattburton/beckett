namespace Beckett.Events;

public readonly struct EventEnvelope(object @event, Dictionary<string, object> metadata, DateTimeOffset? deliverAt)
{
    public object Event { get; } = @event;
    public Dictionary<string, object> Metadata { get; } = metadata;
    public DateTimeOffset? DeliverAt { get; } = deliverAt;
}
