namespace Beckett.MessageStorage;

public readonly struct AppendToStreamResult(long streamVersion)
{
    public long StreamVersion { get; } = streamVersion;
}
