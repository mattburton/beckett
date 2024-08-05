using Npgsql;

namespace Beckett.Subscriptions;

public record Checkpoint(
    string GroupName,
    string Name,
    string StreamName,
    long StreamPosition,
    long StreamVersion,
    CheckpointStatus Status
)
{
    public static Checkpoint? From(NpgsqlDataReader reader)
    {
        return !reader.HasRows
            ? null
            : new Checkpoint(
                reader.GetFieldValue<string>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<string>(2),
                reader.GetFieldValue<long>(3),
                reader.GetFieldValue<long>(4),
                reader.GetFieldValue<CheckpointStatus>(5)
            );
    }
}
