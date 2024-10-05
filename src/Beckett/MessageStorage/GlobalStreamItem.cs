using Beckett.Subscriptions;

namespace Beckett.MessageStorage;

public record GlobalStreamItem(
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    string MessageType
)
{
    public bool AppliesTo(Subscription subscription)
    {
        var categoryMatch = subscription.CategoryMatches(StreamName);
        var messageTypeMatch = subscription.MessageTypes.Contains(MessageType);

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
