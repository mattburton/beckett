using Beckett.Database.Types;
using Beckett.Subscriptions.Models;
using Npgsql;

namespace Beckett.Database;

public static class NpgsqlDataSourceBuilderExtensions
{
    public static NpgsqlDataSourceBuilder AddBeckett(this NpgsqlDataSourceBuilder builder, string schema = PostgresOptions.DefaultSchema)
    {
        builder.MapEnum<CheckpointStatus>(DataTypeNames.CheckpointStatus(schema));
        builder.MapComposite<CheckpointType>(DataTypeNames.Checkpoint(schema));
        builder.MapComposite<MessageType>(DataTypeNames.Message(schema));
        builder.MapComposite<ScheduledMessageType>(DataTypeNames.ScheduledMessage(schema));

        return builder;
    }
}
