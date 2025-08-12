namespace Beckett.Storage.InMemory;

public class InMemoryMessageStorage : IMessageStorage
{
    private readonly List<StreamMessage> _store = [];

    public Task<AppendToStreamResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IReadOnlyList<Message> messages,
        CancellationToken cancellationToken
    )
    {
        var globalPosition = _store.Count;
        var stream = _store.Where(x => x.StreamName == streamName).ToList();
        var streamVersion = stream.Count == 0 ? 0 : stream.Last().StreamPosition;
        var newStreamVersion = streamVersion + messages.Count;

        if (expectedVersion == ExpectedVersion.StreamExists && streamVersion == 0)
        {
            throw new StreamDoesNotExistException();
        }

        if (expectedVersion == ExpectedVersion.StreamDoesNotExist && streamVersion > 0)
        {
            throw new StreamAlreadyExistsException();
        }

        if (expectedVersion.Value > 0 && expectedVersion.Value != streamVersion)
        {
            throw new OptimisticConcurrencyException();
        }

        foreach (var message in messages)
        {
            _store.Add(
                new StreamMessage(
                    Guid.NewGuid().ToString(),
                    streamName,
                    ++streamVersion,
                    ++globalPosition,
                    message.Type,
                    message.Data,
                    message.SerializedMetadata,
                    DateTimeOffset.UtcNow
                )
            );
        }

        return Task.FromResult(new AppendToStreamResult(newStreamVersion));
    }

    public Task<ReadGlobalStreamResult> ReadGlobalStream(
        ReadGlobalStreamOptions options,
        CancellationToken cancellationToken
    )
    {
        var messages = _store.Where(x => x.GlobalPosition > options.LastGlobalPosition).Take(options.BatchSize)
            .ToList();

        var items = messages.Select(x =>
            {
                string? tenant = null;
                string? correlationId = null;

                if (x.Metadata.TryGetProperty("$tenant", out var tenantProperty))
                {
                    tenant = tenantProperty.GetString();
                }

                if (x.Metadata.TryGetProperty("$correlation_id", out var correlationProperty))
                {
                    correlationId = correlationProperty.GetString();
                }

                return new GlobalStreamMessage(
                    Guid.Parse(x.Id),
                    x.StreamName,
                    x.StreamPosition,
                    x.GlobalPosition,
                    x.Type,
                    tenant,
                    correlationId,
                    x.Metadata.GetBytesFromBase64(),
                    x.Timestamp
                );
            }
        ).ToList();

        return Task.FromResult(new ReadGlobalStreamResult(items));
    }

    public Task<ReadStreamResult> ReadStream(
        string streamName,
        ReadStreamOptions options,
        CancellationToken cancellationToken
    )
    {
        var stream = _store.Where(x => x.StreamName == streamName).ToList();
        var streamVersion = stream.Count == 0 ? 0 : stream.Last().StreamPosition;

        if (options.StartingGlobalPosition.HasValue)
        {
            stream = stream.Where(x => x.GlobalPosition > options.StartingGlobalPosition.Value).ToList();
        }

        if (options.EndingGlobalPosition.HasValue)
        {
            stream = stream.Where(x => x.GlobalPosition <= options.EndingGlobalPosition.Value).ToList();
        }

        if (options.StartingStreamPosition.HasValue)
        {
            stream = stream.Where(x => x.StreamPosition > options.StartingStreamPosition.Value).ToList();
        }

        if (options.EndingStreamPosition.HasValue)
        {
            stream = stream.Where(x => x.StreamPosition <= options.EndingStreamPosition.Value).ToList();
        }

        if (options.Types is { Length: > 0 })
        {
            stream = stream.Where(x => options.Types.Contains(x.Type)).ToList();
        }

        if (options.ReadForwards.HasValue && !options.ReadForwards.Value)
        {
            stream.Reverse();
        }

        if (options.Count.HasValue)
        {
            stream = stream.Take(options.Count.Value).ToList();
        }

        return Task.FromResult(new ReadStreamResult(streamName, streamVersion, stream));
    }
}
