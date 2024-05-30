using Beckett.Messages;

namespace Beckett.Subscriptions;

public class Subscription(string name)
{
    internal string Name { get; } = name;
    internal string Category { get; set; } = null!;
    internal Type? HandlerType { get; set; }
    internal string? HandlerName { get; set; }
    internal HashSet<Type> MessageTypes { get; } = [];
    internal HashSet<string> MessageTypeNames { get; private set; } = [];
    internal Func<object, object, CancellationToken, Task>? InstanceMethod { get; set; }
    internal Func<object, CancellationToken, Task>? StaticMethod { get; set; }
    internal StartingPosition StartingPosition { get; set; } = StartingPosition.Latest;
    internal Dictionary<Type, int> MaxRetriesByExceptionType { get; } = new() {{typeof(Exception), 10}};

    internal bool IsCategoryOnly => MessageTypes.Count == 0;

    internal bool CategoryMatches(string streamName) => streamName.StartsWith(Category);

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

    internal int GetMaxRetryCount(Type exceptionType)
    {
        if (!MaxRetriesByExceptionType.TryGetValue(exceptionType, out var maxRetries))
        {
            maxRetries = MaxRetriesByExceptionType[typeof(Exception)];
        }

        return maxRetries;
    }
}
