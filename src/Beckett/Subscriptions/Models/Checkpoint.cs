using Npgsql;

namespace Beckett.Subscriptions.Models;

public record Checkpoint(
    long Id,
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
                reader.GetFieldValue<long>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<string>(2),
                reader.GetFieldValue<string>(3),
                reader.GetFieldValue<long>(4),
                reader.GetFieldValue<long>(5),
                reader.GetFieldValue<CheckpointStatus>(6)
            );
    }
}
