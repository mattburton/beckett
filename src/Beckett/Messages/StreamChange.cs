using Beckett.Subscriptions;

namespace Beckett.Messages;

public record StreamChange(
    string StreamName,
    long StreamVersion,
    long GlobalPosition,
    string[] MessageTypes
)
{
    public bool AppliesTo(Subscription subscription)
    {
        var categoryMatch = subscription.CategoryMatches(StreamName);
        var messageTypeMatch = MessageTypes.Intersect(subscription.MessageTypeNames).Any();

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
