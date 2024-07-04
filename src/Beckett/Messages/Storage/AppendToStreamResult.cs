namespace Beckett.Messages.Storage;

public readonly struct AppendToStreamResult(long streamVersion)
{
    public long StreamVersion { get; } = streamVersion;
}
