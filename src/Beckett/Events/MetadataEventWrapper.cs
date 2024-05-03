namespace Beckett.Events;

public readonly struct MetadataEventWrapper(object @event, Dictionary<string, object> metadata)
{
    public object Event { get; } = @event;
    public Dictionary<string, object> Metadata { get; } = metadata;
}
