using Beckett.Database;
using Beckett.Messages;
using Beckett.Messages.Scheduling;
using Beckett.Subscriptions;

namespace Beckett;

public class BeckettOptions
{
    public const string SectionName = "Beckett";

    public MessageOptions Messages { get; } = new();
    public ScheduledMessageOptions ScheduledMessages { get; } = new();
    public SubscriptionOptions Subscriptions { get; } = new();
    public PostgresOptions Postgres { get; } = new();

    public void UsePostgres(Action<PostgresOptions>? configure = null)
    {
        Postgres.Enabled = true;

        configure?.Invoke(Postgres);
    }
}
