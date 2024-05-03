using Beckett.Events;
using Beckett.Storage.Postgres;
using Beckett.Subscriptions;

namespace Beckett;

public class BeckettOptions
{
    public BeckettOptions(EventOptions events, SubscriptionOptions subscriptions, PostgresOptions postgres)
    {
        Events = events;
        Subscriptions = subscriptions;
        Postgres = postgres;
    }

    public BeckettOptions()
    {
        Events = new EventOptions();
        Subscriptions = new SubscriptionOptions(Events);
        Postgres = new PostgresOptions();
    }

    public EventOptions Events { get; }
    public SubscriptionOptions Subscriptions { get; }
    public PostgresOptions Postgres { get; }

    public void UsePostgres(Action<PostgresOptions> configure)
    {
        Postgres.Enabled = true;

        configure(Postgres);
    }
}
