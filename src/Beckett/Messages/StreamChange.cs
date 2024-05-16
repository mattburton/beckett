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
        if (subscription.IsPatternOnly)
        {
            return subscription.Pattern.IsMatch(StreamName);
        }

        return subscription.Pattern.IsMatch(StreamName) && MessageTypes.Intersect(subscription.MessageTypeNames).Any();
    }
}
