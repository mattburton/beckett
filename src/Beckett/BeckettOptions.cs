using Beckett.Events;
using Beckett.Storage.Postgres;
using Beckett.Subscriptions;

namespace Beckett;

public class BeckettOptions
{
    public const string SectionName = "Beckett";

    public EventOptions Events { get; } = new();
    public SubscriptionOptions Subscriptions { get; } = new();
    public PostgresOptions Postgres { get; } = new();

    public void UsePostgres(Action<PostgresOptions>? configure = null)
    {
        Postgres.Enabled = true;

        configure?.Invoke(Postgres);
    }
}
