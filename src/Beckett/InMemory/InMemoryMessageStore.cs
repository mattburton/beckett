using Beckett.Messages;

namespace Beckett.InMemory;

public class InMemoryMessageStore : IMessageStore
{
    private readonly List<Message> _messages = [];

    public Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<object> messages,
        CancellationToken cancellationToken
    )
    {
        if (expectedVersion.Value == ExpectedVersion.StreamDoesNotExist.Value)
        {
            if (_messages.Exists(x => x.StreamName == streamName))
            {
                throw new StreamAlreadyExistsException("Stream already exists");
            }
        }

        if (expectedVersion.Value == ExpectedVersion.StreamExists.Value)
        {
            if (!_messages.Exists(x => x.StreamName == streamName))
            {
                throw new StreamDoesNotExistException("Stream does not exist");
            }
        }

        var stream = new Stream(
            _messages.Where(x => x.StreamName == streamName).OrderBy(x => x.StreamPosition).ToList()
        );

        if (expectedVersion.Value > 0 && stream.StreamVersion != expectedVersion.Value)
        {
            throw new OptimisticConcurrencyException("Stream version mismatch");
        }

        foreach (var message in messages)
        {
            var streamPosition = stream.StreamVersion + 1;
            var data = message;
            var metadata = new Dictionary<string, object>();

            if (message is MessageMetadataWrapper messageWithMetadata)
            {
                foreach (var item in messageWithMetadata.Metadata)
                {
                    metadata.TryAdd(item.Key, item.Value);
                }

                data = messageWithMetadata.Message;
            }

            var messageToAppend = new Message(
                streamName,
                streamPosition,
                data,
                new Dictionary<string, object>()
            );

            stream.Messages.Add(messageToAppend);

            _messages.Add(messageToAppend);
        }

        return Task.FromResult(new AppendResult(stream.StreamVersion));
    }

    public Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken)
    {
        var stream = new Stream(
            _messages.Where(x => x.StreamName == streamName).OrderBy(x => x.StreamPosition).ToList()
        );

        if (stream.Messages.Count == 0)
        {
            return Task.FromResult(new ReadResult(streamName, 0, new List<object>(), AppendToStream));
        }

        var messages = stream.Messages;

        if (options.StartingStreamPosition.HasValue)
        {
            var count = (int)options.StartingStreamPosition.Value - 1;

            messages = messages.Skip(count).ToList();
        }

        if (!options.ReadForwards.GetValueOrDefault(true))
        {
            messages.Reverse();
        }

        if (options.Count.HasValue)
        {
            messages = messages.Take((int)options.Count.Value).ToList();
        }

        return Task.FromResult(
            new ReadResult(streamName, stream.StreamVersion, messages.Select(x => x.Data).ToList(), AppendToStream)
        );
    }

    private class Stream(List<Message> messages)
    {
        public List<Message> Messages { get; } = messages;

        public long StreamVersion => Messages.Count;
    }

    private class Message(
        string streamName,
        long streamPosition,
        object data,
        Dictionary<string, object> metadata
    )
    {
        public string StreamName { get; } = streamName;
        public long StreamPosition { get; } = streamPosition;
        public object Data { get; } = data;
        public Dictionary<string, object> Metadata { get; } = metadata;
    }
}
