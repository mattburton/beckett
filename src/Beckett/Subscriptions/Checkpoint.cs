using Npgsql;

namespace Beckett.Subscriptions;

public record Checkpoint(
    long Id,
    string GroupName,
    string Name,
    string StreamName,
    long StreamPosition,
    long StreamVersion,
    int RetryAttempts,
    CheckpointStatus Status,
    long? ReplayTargetPosition = null
)
{
    public bool IsRetryOrFailure => Status is CheckpointStatus.Retry or CheckpointStatus.Failed;

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
                reader.GetFieldValue<int>(6),
                reader.GetFieldValue<CheckpointStatus>(7),
                reader.GetFieldValue<long?>(8)
            );
    }
}
