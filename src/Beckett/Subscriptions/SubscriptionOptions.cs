namespace Beckett.Subscriptions;

public class SubscriptionOptions
{
    private readonly Dictionary<string, SubscriptionGroup> _groups = [];

    /// <summary>
    /// Configure whether subscriptions are enabled for this host. Enabling subscriptions will register all the
    /// necessary dependencies and host services that allows Beckett to process them. Defaults to false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Configure the batch size used when initializing subscriptions. This is the number of messages to read per batch
    /// from the global stream while discovering streams and creating checkpoints for them. Setting this to a higher
    /// number allows initialization to proceed more quickly for a large message store, at the expense of additional
    /// overhead reading from it that could affect the performance of the application otherwise. Defaults to 1000.
    /// </summary>
    public int InitializationBatchSize { get; set; } = 1000;

    /// <summary>
    /// Configure the number of concurrent subscriptions Beckett can initialize at one time per host process.
    /// Defaults to 5.
    /// </summary>
    public int InitializationConcurrency { get; set; } = 5;

    /// <summary>
    /// Control whether this host processes all subscriptions, active-only, or replay-only. An example of where this
    /// setting is helpful is when you want to have dedicated services for active vs replay to allow subscriptions to be
    /// replayed without affecting the rest of the system.
    /// </summary>
    public ReplayMode ReplayMode { get; set; } = ReplayMode.All;

    public IReadOnlyList<SubscriptionGroup> Groups => _groups.Values.ToArray();

    public SubscriptionGroup WithSubscriptionGroup(string name, Action<SubscriptionGroup>? configure)
    {
        var subscriptionGroup = new SubscriptionGroup(name);

        configure?.Invoke(subscriptionGroup);

        if (!_groups.TryAdd(name, subscriptionGroup))
        {
            throw new ArgumentException($"Subscription group '{name}' already exists");
        }

        return subscriptionGroup;
    }
}
