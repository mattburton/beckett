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
    /// Configure whether subscription initialization is enabled on this host.
    /// </summary>
    public bool InitializationEnabled { get; set; } = true;

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
