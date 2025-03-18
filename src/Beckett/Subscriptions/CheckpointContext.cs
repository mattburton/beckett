namespace Beckett.Subscriptions;

public record CheckpointContext(
    long Id,
    long? ParentId,
    string GroupName,
    string SubscriptionName,
    string StreamName,
    long StreamVersion,
    long StreamPosition
) : ICheckpointContext
{
    public static ICheckpointContext Empty = new CheckpointContext(
        0,
        null,
        string.Empty,
        string.Empty,
        string.Empty,
        0,
        0
    );
}
