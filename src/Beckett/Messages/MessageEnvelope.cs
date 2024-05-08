namespace Beckett.Messages;

public readonly struct MessageEnvelope(object message, Dictionary<string, object> metadata)
{
    public object Message { get; } = message;
    public Dictionary<string, object> Metadata { get; } = metadata;
}
