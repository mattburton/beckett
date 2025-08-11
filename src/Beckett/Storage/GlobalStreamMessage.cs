using Beckett.Subscriptions;

namespace Beckett.Storage;

public record GlobalStreamMessage(
    Guid Id,
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    string MessageType,
    string? Tenant,
    string? CorrelationId,
    DateTimeOffset Timestamp
)
{
    public bool AppliesTo(Subscription subscription)
    {
        var categoryMatch = subscription.CategoryMatches(StreamName);
        var streamNameMatch = subscription.StreamName == StreamName;
        var messageTypeMatch = subscription.MessageTypeNames.Contains(MessageType);

        if (subscription.IsCategoryOnly)
        {
            return categoryMatch;
        }

        if (subscription.IsStreamNameOnly)
        {
            return streamNameMatch;
        }

        if (subscription.IsMessageTypesOnly)
        {
            return messageTypeMatch;
        }

        if (subscription.IsStreamNameAndMessageTypes)
        {
            return streamNameMatch && messageTypeMatch;
        }

        return categoryMatch && messageTypeMatch;
    }
}
