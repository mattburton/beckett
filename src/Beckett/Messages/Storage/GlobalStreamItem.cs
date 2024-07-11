using Beckett.Subscriptions;

namespace Beckett.Messages.Storage;

public record GlobalStreamItem(
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    Type MessageType
)
{
    public bool AppliesTo(Subscription subscription, IMessageTypeMap messageTypeMap)
    {
        var categoryMatch = subscription.CategoryMatches(StreamName);
        var messageTypeName = messageTypeMap.GetName(MessageType);
        var messageTypeMatch = subscription.MessageTypeNames.Contains(messageTypeName);

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
