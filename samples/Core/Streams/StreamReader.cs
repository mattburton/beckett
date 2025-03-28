using Beckett;

namespace Core.Streams;

public class StreamReader(IMessageStore messageStore) : IStreamReader
{
    public Task<IStream> ReadStream(string streamName, CancellationToken cancellationToken)
    {
        return ReadStream(streamName, ReadOptions.Default, cancellationToken);
    }

    public async Task<IStream> ReadStream(
        string streamName,
        ReadOptions readOptions,
        CancellationToken cancellationToken
    )
    {
        var stream = await messageStore.ReadStream(streamName, readOptions, cancellationToken);

        return new Stream(stream);
    }
}
