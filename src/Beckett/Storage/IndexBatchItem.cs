using Beckett.Subscriptions;

namespace Beckett.Storage;

public record IndexBatchItem(
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    string MessageType,
    string? Tenant,
    DateTimeOffset Timestamp
)
{
    public bool AppliesTo(Subscription subscription)
    {
        var categoryMatch = subscription.CategoryMatches(StreamName);
        var messageTypeMatch = subscription.MessageTypeNames.Contains(MessageType);

        if (subscription.IsCategoryOnly)
        {
            return categoryMatch;
        }

        if (subscription.IsMessageTypesOnly)
        {
            return messageTypeMatch;
        }

        return categoryMatch && messageTypeMatch;
    }
}
