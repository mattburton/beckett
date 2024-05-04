namespace Beckett.ScheduledEvents;

public readonly struct ScheduledEventContext(
    string streamName,
    object data,
    Dictionary<string, object> metadata
) : IScheduledEventContext
{
    public string StreamName { get; } = streamName;
    public object Data { get; } = data;
    public Dictionary<string, object> Metadata { get; } = metadata;
}
