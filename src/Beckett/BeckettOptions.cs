using Beckett.Database;
using Beckett.Messages;
using Beckett.MessageStorage;
using Beckett.Scheduling;
using Beckett.Subscriptions;

namespace Beckett;

public class BeckettOptions
{
    public const string SectionName = "Beckett";

    public MessageOptions Messages { get; } = new();
    public MessageStorageOptions MessageStorage { get; } = new();
    public SchedulingOptions Scheduling { get; } = new();
    public SubscriptionOptions Subscriptions { get; } = new();
    public PostgresOptions Postgres { get; } = new();

    /// <summary>
    /// Configure a custom implementation of <see cref="IMessageStorage"/> for Beckett to use. This will be registered
    /// as a singleton in the container. Defaults to the Postgres implementation if not specified.
    /// </summary>
    /// <typeparam name="T">Custom <see cref="IMessageStorage"/> implementation type</typeparam>
    public void UseMessageStorage<T>() where T : IMessageStorage
    {
        MessageStorage.MessageStorageType = typeof(T);
    }

    /// <summary>
    /// Configure Postgres options beyond the defaults.
    /// </summary>
    /// <param name="configure">Action to configure Postgres options</param>
    public void UsePostgres(Action<PostgresOptions>? configure = null)
    {
        configure?.Invoke(Postgres);
    }

    /// <summary>
    /// Convenience method to enable subscriptions and set the subscription group name in one line. You can also
    /// further configure the subscription options by passing an action.
    /// </summary>
    /// <param name="groupName">Subscription group name for the host</param>
    /// <param name="configure">Action to configure subscription options</param>
    public void WithSubscriptionGroup(string groupName, Action<SubscriptionOptions>? configure = null)
    {
        Subscriptions.Enabled = true;
        Subscriptions.GroupName = groupName;

        configure?.Invoke(Subscriptions);
    }
}
