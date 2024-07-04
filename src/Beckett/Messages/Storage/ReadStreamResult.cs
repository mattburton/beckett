namespace Beckett.Messages.Storage;

public readonly struct ReadStreamResult(
    string streamName,
    long streamVersion,
    IReadOnlyList<MessageResult> messages
)
{
    public string StreamName { get; } = streamName;
    public long StreamVersion { get; } = streamVersion;
    public IReadOnlyList<MessageResult> Messages { get; } = messages;
}
