namespace Beckett.MessageStorage;

public readonly struct MessageStreamResult(
    string streamName,
    long streamVersion,
    IReadOnlyList<MessageResult> streamMessages
)
{
    public string StreamName { get; } = streamName;
    public long StreamVersion { get; } = streamVersion;
    public IReadOnlyList<MessageResult> Messages { get; } = streamMessages;
}
