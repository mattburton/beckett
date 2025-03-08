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
    CheckpointStatus Status
)
{
    public bool IsRetryOrFailure => Status is CheckpointStatus.Retry or CheckpointStatus.Failed;

    public long StartingPositionFor(Subscription subscription)
    {
        return subscription.StreamScope == StreamScope.PerStream ? StreamPosition + 1 : StreamPosition;
    }

    public long RetryStartingPositionFor(Subscription subscription)
    {
        return subscription.StreamScope == StreamScope.PerStream ? StreamPosition : StreamPosition - 1;
    }

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
                reader.GetFieldValue<CheckpointStatus>(7)
            );
    }
}
