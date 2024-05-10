using Beckett.Messages;

namespace Beckett.Subscriptions;

public class Subscription
{
    internal string Name { get; set; } = null!;
    internal Type Type { get; set; } = null!;
    internal HashSet<Type> MessageTypes { get; } = [];
    internal HashSet<string> MessageTypeNames { get; private set; } = [];
    internal Func<object, object, CancellationToken, Task>? InstanceMethod { get; set; }
    internal Func<IMessageContext, CancellationToken, Task>? StaticMethod { get; set; }
    internal bool AcceptsMessageContext { get; set; }

    public StartingPosition StartingPosition { get; set; } = StartingPosition.Latest;
    public int MaxRetryCount { get; set; } = 10;

    public void SubscribeTo<TMessage>() => MessageTypes.Add(typeof(TMessage));

    internal bool SubscribedToMessage(Type messageType) => MessageTypes.Contains(messageType);

    internal void MapMessageTypeNames(IMessageTypeMap messageTypeMap)
    {
        MessageTypeNames = MessageTypes.Select(messageTypeMap.GetName).ToHashSet();
    }
}
