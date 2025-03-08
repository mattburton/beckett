using System.Text.Json;
using Beckett.Subscriptions;

namespace Beckett.MessageStorage;

public record StreamMessage(
    string Id,
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    string Type,
    JsonElement Data,
    JsonElement Metadata,
    DateTimeOffset Timestamp
)
{
    public bool AppliesTo(Subscription subscription)
    {
        var categoryMatch = subscription.CategoryMatches(StreamName);
        var messageTypeMatch = subscription.MessageTypeNames.Contains(Type);

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
