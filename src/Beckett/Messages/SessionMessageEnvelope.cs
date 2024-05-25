namespace Beckett.Messages;

public readonly struct SessionMessageEnvelope(string streamName, object message, Dictionary<string, object> metadata, long expectedVersion)
{
    public string StreamName { get; } = streamName;
    public object Message { get; } = message;
    public Dictionary<string, object> Metadata { get; } = metadata;
    public long ExpectedVersion { get; } = expectedVersion;
}
