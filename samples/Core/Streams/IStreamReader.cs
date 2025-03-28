using Beckett;

namespace Core.Streams;

public interface IStreamReader
{
    Task<IStream> ReadStream(IStreamName streamName, CancellationToken cancellationToken) =>
        ReadStream(streamName.StreamName(), cancellationToken);

    Task<IStream> ReadStream(string streamName, CancellationToken cancellationToken);

    Task<IStream> ReadStream(
        IStreamName streamName,
        ReadOptions readOptions,
        CancellationToken cancellationToken
    ) => ReadStream(streamName.StreamName(), readOptions, cancellationToken);

    Task<IStream> ReadStream(
        string streamName,
        ReadOptions readOptions,
        CancellationToken cancellationToken
    );
}
