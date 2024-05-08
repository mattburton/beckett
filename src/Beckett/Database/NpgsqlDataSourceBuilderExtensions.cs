using Beckett.Database.Types;
using Npgsql;

namespace Beckett.Database;

public static class NpgsqlDataSourceBuilderExtensions
{
    public static NpgsqlDataSourceBuilder AddBeckett(this NpgsqlDataSourceBuilder builder, string schema = PostgresOptions.DefaultSchema)
    {
        builder.MapComposite<CheckpointType>($"{schema}.checkpoint");
        builder.MapComposite<MessageType>($"{schema}.message");
        builder.MapComposite<ScheduledMessageType>($"{schema}.scheduled_message");

        return builder;
    }
}
