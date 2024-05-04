using Beckett.Database;
using Beckett.Events;
using Beckett.ScheduledEvents;
using Beckett.Subscriptions;

namespace Beckett;

public class BeckettOptions
{
    public const string SectionName = "Beckett";

    public EventOptions Events { get; } = new();
    public ScheduledEventOptions ScheduledEvents { get; set; } = new();
    public SubscriptionOptions Subscriptions { get; } = new();
    public PostgresOptions Postgres { get; } = new();

    public void UsePostgres(Action<PostgresOptions>? configure = null)
    {
        Postgres.Enabled = true;

        configure?.Invoke(Postgres);
    }
}
