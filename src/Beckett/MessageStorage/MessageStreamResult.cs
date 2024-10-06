namespace Beckett.MessageStorage;

public readonly struct MessageStreamResult(
    string streamName,
    long streamVersion,
    IReadOnlyList<StreamMessage> streamMessages
)
{
    public string StreamName { get; } = streamName;
    public long StreamVersion { get; } = streamVersion;
    public IReadOnlyList<StreamMessage> Messages { get; } = streamMessages;
}
