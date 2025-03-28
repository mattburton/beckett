using Beckett;
using Beckett.MessageStorage;
using Beckett.MessageStorage.InMemory;

namespace Core.Testing;

public class FakeMessageStore : IMessageStore
{
    private readonly InMemoryMessageStorage _storage = new();
    private readonly Dictionary<string, long> _startingPositions = new();

    public async Task<IAppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<Message> messages,
        CancellationToken cancellationToken
    )
    {
        var result = await _storage.AppendToStream(streamName, expectedVersion, messages.ToList(), cancellationToken);

        return new AppendResult(result.StreamVersion);
    }

    public async Task<IMessageStream> ReadStream(
        string streamName,
        ReadOptions options,
        CancellationToken cancellationToken
    )
    {
        var result = await _storage.ReadStream(streamName, ReadStreamOptions.From(options), cancellationToken);

        return new MessageStream(
            result.StreamName,
            result.StreamVersion,
            result.StreamMessages.Select(MessageContext.From).ToList(),
            AppendToStream
        );
    }

    public void HasExistingMessages(string streamName, params object[] messages)
    {
        AppendToStream(
            streamName,
            ExpectedVersion.Any,
            messages.Select(x => new Message(x)),
            CancellationToken.None
        ).GetAwaiter().GetResult();
    }

    public object? LatestMessage(string streamName)
    {
        var stream = GetLatestMessagesForStream(streamName);

        return stream.Messages.LastOrDefault();
    }

    public IReadOnlyList<object> LatestMessages(string streamName)
    {
        var stream = GetLatestMessagesForStream(streamName);

        return stream.Messages;
    }

    private MessageStream GetLatestMessagesForStream(string streamName)
    {
        var startingStreamPosition = _startingPositions.GetValueOrDefault(streamName, 0);

        var result = _storage.ReadStream(streamName, new ReadStreamOptions
        {
            StartingStreamPosition = startingStreamPosition
        }, CancellationToken.None).GetAwaiter().GetResult();

        return new MessageStream(
            result.StreamName,
            result.StreamVersion,
            result.StreamMessages.Select(MessageContext.From).ToList(),
            AppendToStream
        );
    }
}
