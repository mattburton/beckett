namespace Beckett.Subscriptions;

/// <summary>
/// Exception that subscription batch handlers can throw to tell Beckett the position of the message which caused an
/// error. For subscriptions that are partitioned by stream this should be the stream position, otherwise it should be
/// the global position.
/// </summary>
/// <param name="position">The stream or global position of the failed message</param>
/// <param name="exception">Error that occurred</param>
public class BatchHandlerException(long position, Exception exception) : Exception(null, exception)
{
    public long Position => position;
}
