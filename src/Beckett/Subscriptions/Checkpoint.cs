using Npgsql;

namespace Beckett.Subscriptions;

public record Checkpoint(
    long Id,
    int SubscriptionId,
    string StreamName,
    long StreamPosition,
    long StreamVersion,
    int RetryAttempts,
    CheckpointStatus Status
)
{
    public bool IsRetryOrFailure => Status is CheckpointStatus.Retry or CheckpointStatus.Failed;

    public long StartingStreamPosition => StreamPosition + 1;

    public static Checkpoint? From(NpgsqlDataReader reader)
    {
        return !reader.HasRows
            ? null
            : new Checkpoint(
                reader.GetFieldValue<long>(0),
                reader.GetFieldValue<int>(1),
                reader.GetFieldValue<string>(2),
                reader.GetFieldValue<long>(3),
                reader.GetFieldValue<long>(4),
                reader.GetFieldValue<int>(5),
                reader.GetFieldValue<CheckpointStatus>(6)
            );
    }
}
