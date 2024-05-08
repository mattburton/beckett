namespace Beckett.Messages.Scheduling;

public readonly struct ScheduledMessageEnvelope(
    object message,
    Dictionary<string, object> metadata,
    DateTimeOffset deliverAt
)
{
    public object Message { get; } = message;
    public Dictionary<string, object> Metadata { get; } = metadata;
    public DateTimeOffset DeliverAt { get; } = deliverAt;
}
