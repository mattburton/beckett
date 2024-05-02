namespace Beckett.Subscriptions;

public class SubscriptionOptions
{
    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 500;
    public int Concurrency { get; set; } = 20;
    public int BufferSize { get; set; } = 10000;
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(10);

    public void AddSubscription<THandler, TEvent>(
        string name,
        Func<THandler, TEvent, CancellationToken, Task> handler,
        Action<Subscription>? configure = null
    )
    {
        SubscriptionRegistry.AddSubscription(name, handler, configure);
    }
}