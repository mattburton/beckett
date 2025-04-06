using Beckett;
using Beckett.MessageStorage;
using Beckett.MessageStorage.InMemory;
using Core.Contracts;

namespace Core.Streams;

public class FakeStreamReader : IStreamReader
{
    private readonly InMemoryMessageStorage _storage = new();

    public Task<IStream> ReadStream(string streamName, CancellationToken cancellationToken)
    {
        return ReadStream(streamName, ReadOptions.Default, CancellationToken.None);
    }

    public async Task<IStream> ReadStream(
        string streamName,
        ReadOptions options,
        CancellationToken cancellationToken
    )
    {
        var result = await _storage.ReadStream(streamName, ReadStreamOptions.From(options), cancellationToken);

        return new Stream(
            new MessageStream(
                result.StreamName,
                result.StreamVersion,
                result.StreamMessages.Select(MessageContext.From).ToList(),
                NoOpAppendToStream
            )
        );
    }

    public void HasExistingStream(IStreamName streamName, params IEventType[] messages)
    {
        _storage.AppendToStream(
            streamName.StreamName(),
            ExpectedVersion.Any,
            messages.Select(x => new Message(x)).ToList(),
            CancellationToken.None
        ).GetAwaiter().GetResult();
    }

    private static Task<IAppendResult> NoOpAppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<Message> messages,
        CancellationToken cancellationToken
    ) => Task.FromResult<IAppendResult>(new AppendResult(-1));
}
