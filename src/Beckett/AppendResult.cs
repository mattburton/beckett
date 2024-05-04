namespace Beckett;

public readonly struct AppendResult(long streamVersion)
{
    public long StreamVersion { get; } = streamVersion;
}
