using Beckett.Database.Types;
using Beckett.Subscriptions;
using Beckett.Subscriptions.Retries;
using Npgsql;

namespace Beckett.Database;

public static class NpgsqlDataSourceBuilderExtensions
{
    public static NpgsqlDataSourceBuilder AddBeckett(
        this NpgsqlDataSourceBuilder builder,
        string schema = PostgresOptions.DefaultSchema
    )
    {
        builder.MapComposite<CheckpointType>(DataTypeNames.Checkpoint(schema));
        builder.MapComposite<MessageType>(DataTypeNames.Message(schema));
        builder.MapComposite<ScheduledMessageType>(DataTypeNames.ScheduledMessage(schema));

        builder.MapEnum<CheckpointStatus>(DataTypeNames.CheckpointStatus(schema));
        builder.MapEnum<RetryStatus>(DataTypeNames.RetryStatus(schema));

        return builder;
    }
}
