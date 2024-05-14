using Beckett.Messages;

namespace Beckett.Subscriptions;

public class Subscription(string name)
{
    internal string Name { get; } = name;
    internal string Topic { get; set; } = null!;
    internal Type Type { get; set; } = null!;
    internal HashSet<Type> MessageTypes { get; } = [];
    internal HashSet<string> MessageTypeNames { get; private set; } = [];
    internal Func<object, object, CancellationToken, Task>? InstanceMethod { get; set; }
    internal Func<object, CancellationToken, Task>? StaticMethod { get; set; }
    internal bool AcceptsMessageContext { get; set; }
    internal StartingPosition StartingPosition { get; set; } = StartingPosition.Latest;
    internal int MaxRetryCount { get; set; } = 10;

    internal bool SubscribedToMessage(Type messageType) => MessageTypes.Contains(messageType);

    internal void MapMessageTypeNames(IMessageTypeMap messageTypeMap)
    {
        MessageTypeNames = MessageTypes.Select(messageTypeMap.GetName).ToHashSet();
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
}
