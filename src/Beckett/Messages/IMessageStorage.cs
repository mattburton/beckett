using Beckett.Subscriptions;

namespace Beckett.Messages;

public interface IMessageStorage
{
    Task<AppendResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IEnumerable<MessageEnvelope> messages,
        CancellationToken cancellationToken
    );

    Task<ReadResult> ReadStream(string streamName, ReadOptions options, CancellationToken cancellationToken);

    Task<IReadOnlyList<StreamChange>> ReadStreamChanges(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    );
}

public record StreamChange(string StreamName, long StreamVersion, long GlobalPosition, string[] MessageTypes)
{
    public bool AppliesTo(Subscription subscription)
    {
        return MessageTypes.Intersect(subscription.MessageTypeNames).Any();
    }
}
