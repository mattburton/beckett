namespace Beckett;

public static class MessageStoreExtensions
{
    public static Task<AppendResult> AppendToStream(
        this IMessageStore messageStore,
        string streamName,
        ExpectedVersion expectedVersion,
        object message,
        CancellationToken cancellationToken
    )
    {
        return messageStore.AppendToStream(streamName, expectedVersion, [message], cancellationToken);
    }

    public static Task<ReadResult> ReadStream(
        this IMessageStore messageStore,
        string streamName,
        CancellationToken cancellationToken
    )
    {
        return messageStore.ReadStream(streamName, ReadOptions.Default, cancellationToken);
    }
}
