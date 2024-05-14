using Beckett.Subscriptions;

namespace Beckett.Messages;

public interface IMessageStorage
{
    Task<AppendResult> AppendToStream(
        string topic,
        string streamId,
        ExpectedVersion expectedVersion,
        IEnumerable<MessageEnvelope> messages,
        CancellationToken cancellationToken
    );

    Task<ReadResult> ReadStream(
        string topic,
        string streamId,
        ReadOptions options,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<StreamChange>> ReadStreamChanges(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    );
}

public record StreamChange(
    string Topic,
    string StreamId,
    long StreamVersion,
    long GlobalPosition,
    string[] MessageTypes
)
{
    public bool AppliesTo(Subscription subscription)
    {
        return string.Equals(Topic, subscription.Topic, StringComparison.OrdinalIgnoreCase) &&
               MessageTypes.Intersect(subscription.MessageTypeNames).Any();
    }
}
