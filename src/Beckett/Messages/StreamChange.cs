using Beckett.Subscriptions;

namespace Beckett.Messages;

public record StreamChange(
    string StreamName,
    long StreamVersion,
    long GlobalPosition,
    Type[] MessageTypes
)
{
    public bool AppliesTo(Subscription subscription, IMessageTypeMap messageTypeMap)
    {
        var categoryMatch = subscription.CategoryMatches(StreamName);
        var messageTypeNames = MessageTypes.Select(messageTypeMap.GetName).ToArray();
        var messageTypeMatch = messageTypeNames.Intersect(subscription.MessageTypeNames).Any();

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
