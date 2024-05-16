using Beckett.Subscriptions;

namespace Beckett.Messages;

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
        if (subscription.IsTopicOnly)
        {
            return string.Equals(Topic, subscription.Topic, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(Topic, subscription.Topic, StringComparison.OrdinalIgnoreCase) &&
               MessageTypes.Intersect(subscription.MessageTypeNames, StringComparer.OrdinalIgnoreCase).Any();
    }
}
