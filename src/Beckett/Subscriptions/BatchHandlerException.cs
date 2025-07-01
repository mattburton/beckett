namespace Beckett.Subscriptions;

/// <summary>
/// Exception that subscription batch handlers can throw to tell Beckett the stream position of the message which
/// caused an error.
/// </summary>
/// <param name="streamPosition">The stream position of the failed message</param>
/// <param name="exception">Error that occurred</param>
public class BatchHandlerException(long streamPosition, Exception exception) : Exception(null, exception)
{
    public long StreamPosition => streamPosition;
}
