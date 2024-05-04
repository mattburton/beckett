using Beckett.Database.Types;
using Npgsql;

namespace Beckett.Database;

public static class NpgsqlDataSourceBuilderExtensions
{
    public static NpgsqlDataSourceBuilder AddBeckett(this NpgsqlDataSourceBuilder builder, string schema = PostgresOptions.DefaultSchema)
    {
        builder.MapComposite<CheckpointType>($"{schema}.checkpoint");
        builder.MapComposite<EventType>($"{schema}.event");
        builder.MapComposite<ScheduledEventType>($"{schema}.scheduled_event");

        return builder;
    }
}
