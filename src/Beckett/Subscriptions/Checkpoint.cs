using Npgsql;

namespace Beckett.Subscriptions;

public record Checkpoint(
    long Id,
    long? ParentId,
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

    public bool IsGlobalScoped(Subscription subscription) =>
        subscription.StreamScope == StreamScope.GlobalStream && ParentId == null;

    public long StartingPositionFor(Subscription subscription)
    {
        return subscription.StreamScope == StreamScope.PerStream || ParentId.HasValue
            ? StreamPosition + 1
            : StreamPosition;
    }

    public long RetryStartingPositionFor(Subscription subscription)
    {
        return subscription.StreamScope == StreamScope.PerStream || ParentId.HasValue
            ? StreamPosition
            : StreamPosition - 1;
    }

    public static Checkpoint? From(NpgsqlDataReader reader)
    {
        return !reader.HasRows
            ? null
            : new Checkpoint(
                reader.GetFieldValue<long>(0),
                reader.IsDBNull(1) ? null : reader.GetFieldValue<long>(1),
                reader.GetFieldValue<string>(2),
                reader.GetFieldValue<string>(3),
                reader.GetFieldValue<string>(4),
                reader.GetFieldValue<long>(5),
                reader.GetFieldValue<long>(6),
                reader.GetFieldValue<int>(7),
                reader.GetFieldValue<CheckpointStatus>(8)
            );
    }

    public ICheckpointContext ToContext() => new CheckpointContext(
        Id,
        ParentId,
        GroupName,
        Name,
        StreamName,
        StreamVersion,
        StreamPosition
    );
}
