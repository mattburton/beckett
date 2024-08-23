using Beckett.Messages;

namespace Beckett.Subscriptions;

public class Subscription(string name)
{
    internal string Name { get; } = name;
    internal string? Category { get; set; }
    internal Type? HandlerType { get; set; }
    internal string? HandlerName { get; set; }
    internal HashSet<Type> MessageTypes { get; } = [];
    internal HashSet<string> MessageTypeNames { get; private set; } = [];
    internal Func<object, object, CancellationToken, Task>? InstanceMethod { get; set; }
    internal Func<object, CancellationToken, Task>? StaticMethod { get; set; }
    internal StartingPosition StartingPosition { get; set; } = StartingPosition.Latest;
    internal Dictionary<Type, int> MaxRetriesByExceptionType { get; } = [];

    internal bool IsCategoryOnly => Category != null && MessageTypes.Count == 0;

    internal bool IsMessageTypesOnly => Category == null && MessageTypes.Count > 0;

    internal bool CategoryMatches(string streamName) => Category != null && streamName.StartsWith(Category);

    internal bool SubscribedToMessage(Type messageType) => MessageTypes.Contains(messageType) || IsCategoryOnly;

    internal void MapMessageTypeNames(IMessageTypeMap messageTypeMap) =>
        MessageTypeNames = MessageTypes.Select(messageTypeMap.GetName).ToHashSet();

    internal void EnsureHandlerIsConfigured()
    {
        if (StaticMethod == null && InstanceMethod == null)
        {
            throw new InvalidOperationException($"The subscription {Name} does not have a handler configured.");
        }
    }

    internal void EnsureOnlyHandlerMessageTypeIsMapped<TMessage>()
    {
        if (MessageTypes.Count > 1)
        {
            throw new InvalidOperationException(
                $"The subscription {Name} is only expecting the message type {typeof(TMessage).Name} and has additional types mapped to it."
            );
        }
    }

    internal int GetAdvisoryLockId(string groupName) => $"{groupName}:{Name}".GetDeterministicHashCode();

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
