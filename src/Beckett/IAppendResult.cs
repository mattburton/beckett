namespace Beckett;

public interface IAppendResult
{
    /// <summary>
    /// The version - new max stream position - of the stream after successfully appending one or more messages
    /// </summary>
    long StreamVersion { get; }
}

public readonly struct AppendResult(long streamVersion) : IAppendResult
{
    public long StreamVersion { get; } = streamVersion;
}
