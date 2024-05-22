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
        if (subscription.IsCategoryOnly)
        {
            return subscription.CategoryMatches(StreamName);
        }

        return subscription.CategoryMatches(StreamName) && MessageTypes.Intersect(subscription.MessageTypeNames).Any();
    }
}
