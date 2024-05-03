using Beckett.Storage.Postgres.Types;
using Npgsql;

namespace Beckett.Storage.Postgres;

public static class NpgsqlDataSourceBuilderExtensions
{
    public static NpgsqlDataSourceBuilder AddBeckett(this NpgsqlDataSourceBuilder builder, string schema = PostgresOptions.DefaultSchema)
    {
        builder.MapComposite<NewEvent>($"{schema}.new_event");
        builder.MapComposite<NewScheduledEvent>($"{schema}.new_scheduled_event");

        return builder;
    }
}
