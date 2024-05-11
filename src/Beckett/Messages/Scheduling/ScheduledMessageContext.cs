namespace Beckett.Messages.Scheduling;

public readonly struct ScheduledMessageContext(
    string topic,
    string streamId,
    object data,
    Dictionary<string, object> metadata
) : IScheduledMessageContext
{
    public string Topic { get; } = topic;
    public string StreamId { get; } = streamId;
    public object Message { get; } = data;
    public Dictionary<string, object> Metadata { get; } = metadata;
}
