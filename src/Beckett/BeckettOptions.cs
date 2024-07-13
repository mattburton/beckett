using Beckett.Database;
using Beckett.Messages;
using Beckett.Messages.Storage;
using Beckett.Scheduling;
using Beckett.Subscriptions;

namespace Beckett;

public class BeckettOptions
{
    public const string SectionName = "Beckett";

    public MessageOptions Messages { get; } = new();
    public SchedulingOptions Scheduling { get; } = new();
    public SubscriptionOptions Subscriptions { get; } = new();
    public PostgresOptions Postgres { get; } = new();

    public void UseMessageStorage<T>() where T : IMessageStorage
    {
        Postgres.MessageStorageType = typeof(T);
    }

    public void UsePostgres(Action<PostgresOptions>? configure = null)
    {
        Postgres.Enabled = true;

        configure?.Invoke(Postgres);
    }

    public void WithSubscriptions(string groupName, Action<SubscriptionOptions>? configure = null)
    {
        Subscriptions.Enabled = true;
        Subscriptions.GroupName = groupName;

        configure?.Invoke(Subscriptions);
    }
}
