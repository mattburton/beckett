namespace Beckett.Messages.Scheduling;

public readonly struct ScheduledMessageContext(
    string streamName,
    object data,
    Dictionary<string, object> metadata
) : IScheduledMessageContext
{
    public string StreamName { get; } = streamName;
    public object Message { get; } = data;
    public Dictionary<string, object> Metadata { get; } = metadata;
}
