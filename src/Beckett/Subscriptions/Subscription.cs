namespace Beckett.Subscriptions;

public class Subscription
{
    internal string Name { get; set; } = null!;
    internal Type Type { get; set; } = null!;
    internal HashSet<Type> EventTypes { get; } = [];
    internal Func<object, object, CancellationToken, Task>? Handler { get; set; }

    public StartingPosition StartingPosition { get; set; } = StartingPosition.Latest;

    public void SubscribeTo<TEvent>() => EventTypes.Add(typeof(TEvent));

    internal bool SubscribedToEvent(Type eventType) => EventTypes.Contains(eventType);
}