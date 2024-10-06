namespace Beckett.Scheduling;

public readonly struct ScheduledMessageContext(
    string streamName,
    Message message
)
{
    public string StreamName { get; } = streamName;
    public Message Message { get; } = message;
}
