namespace Beckett.Subscriptions;

public class Subscription
{
    internal string Name { get; set; } = null!;
    internal Type Type { get; set; } = null!;
    internal HashSet<Type> MessageTypes { get; } = [];
    internal Func<object, object, CancellationToken, Task>? Handler { get; set; }
    internal bool MessageContextHandler { get; set; }

    public StartingPosition StartingPosition { get; set; } = StartingPosition.Latest;
    public int MaxRetryCount { get; set; } = 10;

    public void SubscribeTo<TMessage>() => MessageTypes.Add(typeof(TMessage));

    internal bool SubscribedToMessage(Type messageType) => MessageTypes.Contains(messageType);
}
