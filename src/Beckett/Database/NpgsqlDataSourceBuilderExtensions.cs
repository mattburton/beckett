using Beckett.Database.Types;
using Beckett.Subscriptions;
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
        builder.MapComposite<RetryType>(DataTypeNames.Retry(schema));
        builder.MapComposite<StreamMetadataType>(DataTypeNames.StreamIndex(schema));
        builder.MapComposite<MessageMetadataType>(DataTypeNames.MessageIndex(schema));

        builder.MapEnum<CheckpointStatus>(DataTypeNames.CheckpointStatus(schema));
        builder.MapEnum<SubscriptionStatus>(DataTypeNames.SubscriptionStatus(schema));

        return builder;
    }
}
