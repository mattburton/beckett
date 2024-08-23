namespace Beckett;

public readonly struct AppendResult(long streamVersion)
{
    /// <summary>
    /// The version - new max stream position - of the stream after successfully appending one or more messages
    /// </summary>
    public long StreamVersion { get; } = streamVersion;
}
