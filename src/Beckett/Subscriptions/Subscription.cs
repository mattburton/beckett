using Beckett.Messages;

namespace Beckett.Subscriptions;

public class Subscription(string name)
{
    internal string Name { get; } = name;
    internal string? Category { get; set; }
    internal HashSet<Type> MessageTypes { get; } = [];
    internal HashSet<string> MessageTypeNames { get; } = [];
    internal Delegate? HandlerDelegate { get; set; }
    internal SubscriptionHandler Handler { get; private set; } = null!;
    internal string? HandlerName { get; set; }
    internal StartingPosition StartingPosition { get; set; } = StartingPosition.Latest;
    internal Dictionary<Type, int> MaxRetriesByExceptionType { get; } = [];
    internal int Priority { get; set; } = int.MaxValue;

    internal bool IsCategoryOnly => Category != null && MessageTypeNames.Count == 0;

    internal bool IsMessageTypesOnly => Category == null && MessageTypeNames.Count > 0;

    internal bool CategoryMatches(string streamName) => Category != null && streamName.StartsWith(Category);

    internal void RegisterMessageType<T>() => RegisterMessageType(typeof(T));

    internal void RegisterMessageType(Type messageType)
    {
        MessageTypeNames.Add(MessageTypeMap.GetName(messageType));
        MessageTypes.Add(messageType);
    }

    internal bool SubscribedToMessage(string messageType) => IsCategoryOnly || MessageTypeNames.Contains(messageType);

    internal void BuildHandler()
    {
        if (HandlerDelegate == null)
        {
            throw new InvalidOperationException($"The subscription {Name} does not have a handler configured.");
        }

        Handler = new SubscriptionHandler(this, HandlerDelegate);
    }

    internal string GetAdvisoryLockKey(string groupName) => $"{groupName}:{Name}";

    internal int GetMaxRetryCount(BeckettOptions options, Type exceptionType)
    {
        var defaultIsConfigured = options.Subscriptions.MaxRetriesByExceptionType.TryGetValue(
            exceptionType,
            out var defaultMaxRetries
        );

        var subscriptionIsConfigured = MaxRetriesByExceptionType.TryGetValue(
            exceptionType,
            out var subscriptionMaxRetries
        );

        if (subscriptionIsConfigured)
        {
            return subscriptionMaxRetries;
        }

        return defaultIsConfigured
            ? defaultMaxRetries
            : options.Subscriptions.MaxRetriesByExceptionType[typeof(Exception)];
    }
}
