using Beckett.Database;
using Beckett.Messages;
using Beckett.Scheduling;
using Beckett.Subscriptions;

namespace Beckett;

public class BeckettOptions
{
    public const string SectionName = "Beckett";

    public string ApplicationName { get; set; } = "default";
    public MessageOptions Messages { get; } = new();
    public SchedulingOptions Scheduling { get; } = new();
    public SubscriptionOptions Subscriptions { get; } = new();
    public PostgresOptions Postgres { get; } = new();

    public void UsePostgres(Action<PostgresOptions>? configure = null)
    {
        Postgres.Enabled = true;

        configure?.Invoke(Postgres);
    }
}
